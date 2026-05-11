using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonoBase.Data;
using MonoBase.Models;
using MonoBase.Services;

namespace MonoBase.Controllers;

public class DatabaseController : Controller
{
    private const string CollectionMarkerPrefix = "__collection__";
    private readonly AppDbContext _context;
    private readonly IDatabaseService _dbService;

    public DatabaseController(IDatabaseService dbService, AppDbContext context)
    {
        _dbService = dbService;
        _context = context;
    }

    public IActionResult Index()
    {
        // 1. Session'dan tam yol (C:\...) DEĞİL, sadece dosya adını (db_test.db) alıyoruz.
        var dbFileName = HttpContext.Session.GetString("UserDbPath");

        if (string.IsNullOrEmpty(dbFileName))
            return RedirectToAction("Login", "Auth");

        try
        {
            // 2. GetUserContext artık içeride Path.Combine kullanarak 
            // Pardus'taki gerçek klasörü dosya adıyla birleştiriyor.
            using var userDb = _dbService.GetUserContext(dbFileName);

            // 3. Verileri çekiyoruz
            var collections = userDb.Collections
                .Select(x => x.CollectionName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return View(collections);
        }
        catch (Exception ex)
        {
            // Detaylı hata mesajını terminalde görmek için:
            Console.WriteLine($"Pardus Veritabanı Hatası: {ex.Message}");

            TempData["Error"] = "Veritabanına erişilemedi. Lütfen tekrar giriş yapın.";
            return View(new List<string>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCollection(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Tablo adı boş olamaz.";
            return RedirectToAction("Index");
        }

        var dbPath = HttpContext.Session.GetString("UserDbPath");

        if (string.IsNullOrEmpty(dbPath))
            return RedirectToAction("Login", "Auth");

        try
        {
            using var userDb = _dbService.GetUserContext(dbPath);

            name = name.Trim();

            // Aynı tablo var mı kontrolü
            bool exists = await userDb.Collections
                .AnyAsync(x => x.CollectionName == name);

            if (exists)
            {
                TempData["Error"] = "Bu tablo zaten mevcut.";
                return RedirectToAction("Index");
            }

            userDb.Collections.Add(new DynamicEntry
            {
                Id = CollectionMarkerPrefix + name,
                CollectionName = name,
                JsonData = "{}",
                CreatedAt = DateTime.Now
            });
            await userDb.SaveChangesAsync();



            TempData["Success"] = "Tablo başarıyla oluşturuldu.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
        }

        return RedirectToAction("Index");
    }
    // Tablo Detaylarını ve Verileri Getir
    // Tablo detaylarını göster
    // Tablo detaylarını ve içindeki verileri getir
    public async Task<IActionResult> TableDetails(string tableName)
    {
        var dbPath = HttpContext.Session.GetString("UserDbPath");
        using var userDb = _dbService.GetUserContext(dbPath);

        // Yanlış: .Select(x => x.CollectionName) <-- Bunu yapma!
        // Doğru: Nesnenin tamamını çek
        var data = await userDb.Collections
            .Where(x => x.CollectionName == tableName)
            .Where(x => !x.Id.StartsWith(CollectionMarkerPrefix))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        ViewBag.TableName = tableName;
        return View(data); // List<DynamicEntry> gidiyor
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRow(string id, string tableName) // id artık string
    {
        // 1. Session'dan kullanıcının özel DB yolunu al
        var dbPath = HttpContext.Session.GetString("UserDbPath");
        if (string.IsNullOrEmpty(dbPath)) return RedirectToAction("Login", "Auth");

        try
        {
            // 2. Kullanıcıya özel context'i aç
            using var userDb = _dbService.GetUserContext(dbPath);

            // 3. Veriyi string ID ile bul
            // Firebase mantığında ID'ler benzersiz string dizileridir (Guid)
            var entry = await userDb.Collections
    .FirstOrDefaultAsync(x =>
        x.Id == id &&
        x.CollectionName == tableName &&
        !x.Id.StartsWith(CollectionMarkerPrefix));

            if (entry != null)
            {
                userDb.Collections.Remove(entry);
                await userDb.SaveChangesAsync();
            }
            TempData["SuccessMessage"] = "Veri başarıyla silindi!";
            return RedirectToAction("TableDetails", new { tableName = tableName });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Veri silinirken bir hata oluştu: " + ex.Message;
            return RedirectToAction("TableDetails", new { tableName = tableName });
        }
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRow(string tableName, string jsonData)
    {
        var dbPath = HttpContext.Session.GetString("UserDbPath");
        if (string.IsNullOrEmpty(dbPath)) return RedirectToAction("Login", "Auth");

        try
        {
            // 1. Veritabanı context'ini al
            using var userDb = _dbService.GetUserContext(dbPath);

            // 2. KRİTİK DÜZELTME: Tablo var mı kontrol et, yoksa OLUŞTUR
            // 'no such table: Collections' hatasını bu satır çözer.
            await userDb.Database.EnsureCreatedAsync();

            // 3. Tablo işaretçisini (marker) ekle veya kontrol et
            string markerId = CollectionMarkerPrefix + tableName;
            var existingMarker = await userDb.Collections.AnyAsync(x => x.Id == markerId);

            if (!existingMarker)
            {
                await userDb.Collections.AddAsync(new DynamicEntry
                {
                    Id = markerId,
                    CollectionName = tableName,
                    JsonData = "{}",
                    CreatedAt = DateTime.Now
                });
                await userDb.SaveChangesAsync();
            }

            // 4. Yeni veriyi (row) ekle
            string documentId = Guid.NewGuid().ToString("n");
            var dataObject = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData)
                             ?? new Dictionary<string, object>();

            dataObject["_id"] = documentId;
            var finalJson = JsonSerializer.Serialize(dataObject);

            var newEntry = new DynamicEntry
            {
                Id = documentId,
                CollectionName = tableName,
                JsonData = finalJson,
                CreatedAt = DateTime.Now
            };

            await userDb.Collections.AddAsync(newEntry);
            await userDb.SaveChangesAsync();

            return RedirectToAction("TableDetails", new { tableName = tableName });
        }
        catch (Exception ex)
        {
            // Hata varsa ekranda detaylı görelim
            TempData["Error"] = "Hata oluştu: " + (ex.InnerException?.Message ?? ex.Message);
            return RedirectToAction("TableDetails", new { tableName = tableName });
        }
    }





}
