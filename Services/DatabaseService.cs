using Microsoft.EntityFrameworkCore;
using MonoBase.Data;
using MonoBase.Models;

public interface IDatabaseService
{
    string CreateUserDatabase(string email);
    UserDbContext GetUserContext(string dbFileName); // dbPath yerine dbFileName alacak
}

public class DatabaseService : IDatabaseService
{
    // Pardus/Linux için güvenli klasör yolu (ContentRootPath kullanımı önerilir)
    private readonly string _storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "UserDatabases");

    public string CreateUserDatabase(string email)
    {
        // 1. Güvenli dosya ismi oluştur
        var safeEmail = email.Replace("@", "-").Replace(".", "-");
        var fileName = $"db_{safeEmail}_{Guid.NewGuid().ToString().Substring(0, 8)}.db";

        // 2. Klasörün varlığından emin ol (Linux izinleri için kritik)
        if (!Directory.Exists(_storageFolder))
        {
            Directory.CreateDirectory(_storageFolder);
        }

        // 3. Dosya yolunu işletim sistemine uygun birleştir (Path.Combine '/' kullanır)
        var fullPath = Path.Combine(_storageFolder, fileName);
        
        // 4. SQLite veritabanını ve tablolarını oluştur
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.UseSqlite($"Data Source={fullPath}");

        using (var context = new UserDbContext(optionsBuilder.Options))
        {
            // Pardus'ta tablonun fiziksel olarak oluştuğundan emin olur
            context.Database.EnsureCreated(); 
        }

        // KRİTİK: Veritabanına tam yolu (C:\...) değil, SADECE dosya adını döndür/kaydet.
        return fileName; 
    }

    // Çalışma anında kullanıcının DB'sine bağlanmak için
    public UserDbContext GetUserContext(string dbFileName)
    {
        // Çalışma anında sistemin o anki yolu ile dosya adını birleştirir
        var fullPath = Path.Combine(_storageFolder, dbFileName);
        
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.UseSqlite($"Data Source={fullPath}");
        
        return new UserDbContext(optionsBuilder.Options);
    }
}