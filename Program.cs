using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MonoBase.Data;
using MonoBase.Models;
using MonoBase.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı Yolunu Dinamik Yap (Pardus uyumu için)
var mainDbPath = Path.Combine(builder.Environment.ContentRootPath, "MainSystem.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={mainDbPath}"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddDistributedMemoryCache();

// 2. DataProtection klasörünü güvenli oluştur
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
if (!Directory.Exists(keysFolder)) Directory.CreateDirectory(keysFolder);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(3);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 3. UserDatabases klasörünü uygulama başlamadan hazırla
var dbFolder = Path.Combine(builder.Environment.ContentRootPath, "UserDatabases");
if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);

// 4. Veritabanı Migration işlemini güvenli yap
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration Hatası: {ex.Message}");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();