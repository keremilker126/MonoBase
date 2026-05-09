using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonoBase.Data;
using MonoBase.Models;
using MonoBase.Services;

namespace MonoBase.Controllers;

[ApiController]
[Route("api/data")]
public class ApiDataController : ControllerBase
{
    private const string CollectionMarkerPrefix = "__collection__";
    private readonly AppDbContext _context;
    private readonly IDatabaseService _dbService;

    public ApiDataController(AppDbContext context, IDatabaseService dbService)
    {
        _context = context;
        _dbService = dbService;
    }

    [HttpGet("{collectionName}")]
    public async Task<IActionResult> GetDocuments(string collectionName, [FromQuery] string apiKey)
    {
        var user = await FindUserAsync(apiKey);
        if (user == null) return Unauthorized(new { error = "Gecersiz API anahtari." });

        using var userDb = _dbService.GetUserContext(user.UserDbPath!);
        var entries = await userDb.Collections
            .Where(x => x.CollectionName == collectionName)
            .Where(x => !x.Id.StartsWith(CollectionMarkerPrefix))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var documents = entries.Select(ToApiDocument).ToList();
        return Ok(documents);
    }

    [HttpGet("{collectionName}/{id}")]
    public async Task<IActionResult> GetDocument(string collectionName, string id, [FromQuery] string apiKey)
    {
        var user = await FindUserAsync(apiKey);
        if (user == null) return Unauthorized(new { error = "Gecersiz API anahtari." });

        using var userDb = _dbService.GetUserContext(user.UserDbPath!);
        var entry = await userDb.Collections
            .Where(x => x.CollectionName == collectionName)
            .Where(x => x.Id == id)
            .Where(x => !x.Id.StartsWith(CollectionMarkerPrefix))
            .FirstOrDefaultAsync();

        if (entry == null) return NotFound(new { error = "Dokuman bulunamadi." });

        return Ok(ToApiDocument(entry));
    }

    [HttpPost("{collectionName}")]
    public async Task<IActionResult> AddDocument(
     string collectionName,
     [FromQuery] string apiKey,
     [FromBody] JsonElement body)
    {
        var user = await FindUserAsync(apiKey);
        if (user == null) return Unauthorized(new { error = "Gecersiz API anahtari." });

        if (body.ValueKind != JsonValueKind.Object)
            return BadRequest(new { error = "Gonderilen veri JSON obje olmali." });

        try
        {
            using var userDb = _dbService.GetUserContext(user.UserDbPath!);

            // 1. KRİTİK: Tablo yoksa oluştur (Panelde aldığın hatayı önler)
            await userDb.Database.EnsureCreatedAsync();

            // 2. Tablo Marker Kontrolü (Daha güvenli hale getirildi)
            string markerId = CollectionMarkerPrefix + collectionName;
            var existingMarker = await userDb.Collections.AnyAsync(x => x.Id == markerId);

            if (!existingMarker)
            {
                userDb.Collections.Add(new DynamicEntry
                {
                    Id = markerId,
                    CollectionName = collectionName,
                    JsonData = "{}",
                    CreatedAt = DateTime.Now
                });
                // Marker'ı hemen kaydet ki alttaki asıl veriyle çakışmasın
                await userDb.SaveChangesAsync();
            }

            // 3. Veriyi Hazırla
            var documentId = Guid.NewGuid().ToString("n");
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(body.GetRawText())
                ?? new Dictionary<string, object>();

            if (!data.ContainsKey("_id"))
                data["_id"] = documentId;

            var entry = new DynamicEntry
            {
                Id = documentId,
                CollectionName = collectionName,
                JsonData = JsonSerializer.Serialize(data),
                CreatedAt = DateTime.Now
            };

            // 4. Asıl Veriyi Kaydet
            userDb.Collections.Add(entry);
            await userDb.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetDocument),
                new { collectionName, id = entry.Id, apiKey },
                ToApiDocument(entry));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Sistem hatası", details = ex.Message });
        }
    }

    [HttpDelete("{collectionName}/{id}")]
    public async Task<IActionResult> DeleteDocument(string collectionName, string id, [FromQuery] string apiKey)
    {
        var user = await FindUserAsync(apiKey);
        if (user == null) return Unauthorized(new { error = "Gecersiz API anahtari." });

        using var userDb = _dbService.GetUserContext(user.UserDbPath!);
        var entry = await userDb.Collections
            .Where(x => x.CollectionName == collectionName)
            .Where(x => x.Id == id)
            .Where(x => !x.Id.StartsWith(CollectionMarkerPrefix))
            .FirstOrDefaultAsync();

        if (entry == null) return NotFound(new { error = "Dokuman bulunamadi." });

        userDb.Collections.Remove(entry);
        await userDb.SaveChangesAsync();

        return NoContent();
    }

    private async Task<ApplicationUser?> FindUserAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        return await _context.Users.FirstOrDefaultAsync(x =>
            x.ApiKey == apiKey &&
            x.IsEmailConfirmed &&
            !string.IsNullOrEmpty(x.UserDbPath));
    }

    private static object ToApiDocument(DynamicEntry entry)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry.JsonData)
            ?? new Dictionary<string, JsonElement>();

        return new
        {
            id = entry.Id,
            collection = entry.CollectionName,
            createdAt = entry.CreatedAt,
            data
        };
    }
    [HttpPut("{collectionName}/{id}")]
    public async Task<IActionResult> UpdateDocument(
    string collectionName,
    string id,
    [FromQuery] string apiKey,
    [FromBody] JsonElement body)
    {
        var user = await FindUserAsync(apiKey);
        if (user == null) return Unauthorized(new { error = "Gecersiz API anahtari." });

        using var userDb = _dbService.GetUserContext(user.UserDbPath!);

        // Güncellenecek kaydı bul
        var entry = await userDb.Collections
            .Where(x => x.CollectionName == collectionName && x.Id == id)
            .FirstOrDefaultAsync();

        if (entry == null) return NotFound(new { error = "Guncellenecek dokuman bulunamadi." });

        // Gelen yeni veriyi işle
        var newData = JsonSerializer.Deserialize<Dictionary<string, object>>(body.GetRawText());
        if (newData == null) return BadRequest(new { error = "Gecersiz veri formatı." });

        // Mevcut verinin ID'sini koru
        if (!newData.ContainsKey("_id")) newData["_id"] = id;

        // Veritabanını güncelle
        entry.JsonData = JsonSerializer.Serialize(newData);
        // entry.CreatedAt = DateTime.Now; // İstersen güncelleme tarihini de değiştirebilirsin

        await userDb.SaveChangesAsync();

        return Ok(ToApiDocument(entry));
    }
}
