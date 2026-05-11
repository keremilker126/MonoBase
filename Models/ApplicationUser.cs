public class ApplicationUser
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public string? VerificationToken { get; set; } // Maildeki linkte gidecek benzersiz kod
    public string? LoginVerificationToken { get; set; } // Giriş doğrulama kodu (2FA benzeri)
    public string? UserDbPath { get; set; } 
    public string? ApiKey { get; set; } 
    // --- ŞİFRE SIFIRLAMA İÇİN EKLENENLER ---
    public string? ResetToken { get; set; } // Şifre sıfırlama linkindeki kod
    public DateTime? ResetTokenExpires { get; set; } // Kodun son kullanma tarihi
}