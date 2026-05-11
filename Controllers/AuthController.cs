using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonoBase.Data;
using MonoBase.Models;
using MonoBase.Services;

public class AuthController : Controller
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IDatabaseService _dbService;

    public AuthController(AppDbContext context, IEmailService emailService, IDatabaseService dbService)
    {
        _context = context;
        _emailService = emailService;
        _dbService = dbService;
    }

    [HttpGet] public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(string email, string password)
    {
        // 1. E-posta sistemde var mı kontrolü
        var existingUser = await _context.Users.AnyAsync(u => u.Email == email);

        if (existingUser)
        {
            // Kullanıcıyı bilgilendiriyoruz
            ViewBag.Error = "Bu e-posta adresi zaten sisteme kayıtlı. Lütfen farklı bir adres deneyin veya giriş yapın.";
            return View(); // Sayfayı hata mesajıyla birlikte tekrar gösterir
        }

        // 2. Yeni kullanıcıyı oluşturma süreci
        var user = new ApplicationUser
        {
            Email = email,
            PasswordHash = password, // Not: BCrypt kullanmayı unutma!
            VerificationToken = Guid.NewGuid().ToString(),
            ApiKey = Guid.NewGuid().ToString("N"),
            IsEmailConfirmed = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Onay maili gönderimi
        var confirmLink = Url.Action("ConfirmEmail", "Auth", new { token = user.VerificationToken }, Request.Scheme);
        await _emailService.SendVerificationEmailAsync(user.Email, confirmLink);

        return RedirectToAction("Login", new { msg = "Kayıt başarılı! Lütfen e-postanızı onaylayın." });
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);

        if (user == null) return NotFound("Geçersiz doğrulama kodu.");

        if (!user.IsEmailConfirmed)
        {
            // DatabaseService artık sadece "db_isim_guid.db" dönecek şekilde güncellenmeli
            user.UserDbPath = _dbService.CreateUserDatabase(user.Email);
            user.IsEmailConfirmed = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Login", new { msg = "E-posta onaylandı! Giriş yapabilirsiniz." });
    }

    [HttpGet]
    public IActionResult Login(string msg)
    {
        ViewBag.Message = msg;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password);

        if (user == null)
        {
            ViewBag.Error = "Geçersiz e-posta veya şifre.";
            return View();
        }

        if (!user.IsEmailConfirmed)
        {
            ViewBag.Error = "Lütfen önce e-postanızı onaylayın.";
            return View();
        }

        // --- SESSION YÖNETİMİ ---
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserDbPath", user.UserDbPath ?? "");
        HttpContext.Session.SetString("UserApiKey", user.ApiKey ?? "");

        // Burayı Dashboard olarak güncelledik:
        return RedirectToAction("Index", "Database");
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
    
}