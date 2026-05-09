using Microsoft.EntityFrameworkCore;
using MonoBase.Data;
using MonoBase.Models;

public interface IDatabaseService
{
    string CreateUserDatabase(string email);
    UserDbContext GetUserContext(string dbPath);
}

public class DatabaseService : IDatabaseService
{
    public string CreateUserDatabase(string email)
    {
        var safeEmail = email.Replace("@", "-").Replace(".", "-");
        var fileName = $"db_{safeEmail}_{Guid.NewGuid().ToString().Substring(0, 8)}.db";
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "UserDatabases");

        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        var dbPath = Path.Combine(folderPath, fileName);
        
        // Şablon üzerinden tabloyu oluştur
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        using (var context = new UserDbContext(optionsBuilder.Options))
        {
            context.Database.EnsureCreated(); // DynamicEntry tablosunu içine basar
        }

        return dbPath;
    }

    // Çalışma anında kullanıcının DB'sine bağlanmak için
    public UserDbContext GetUserContext(string dbPath)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new UserDbContext(optionsBuilder.Options);
    }
}