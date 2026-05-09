using Microsoft.Extensions.Options;
using MonoBase.Models;
using System.Net;
using System.Net.Mail;

namespace MonoBase.Services
{
    public interface IEmailService
    {
        // Genel mail gönderimi (Hata durumunda false döner)
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        
        // Onay maili gönderimi için özel yardımcı metod
        Task<bool> SendVerificationEmailAsync(string toEmail, string confirmationLink);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            // AppSettings'den gelen verilerin null kontrolü
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // SmtpClient yapılandırması
                using var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = true,
                    Timeout = 10000 // 10 saniye zaman aşımı (Önemli: İstek asılı kalmasın)
                };

                // Mail içeriği oluşturma
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // HTML desteği aktif
                };

                mailMessage.To.Add(toEmail);

                // Gönderim
                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Hata olduğunda konsola yazdır (Geliştirme aşamasında hatayı görmeni sağlar)
                Console.WriteLine($"SMTP Hatası: {ex.Message}");
                return false;
            }
        }

        // Onay maili taslağını burada yönetmek kod temizliği sağlar
        public async Task<bool> SendVerificationEmailAsync(string toEmail, string confirmationLink)
        {
            string subject = "Monobase — Hesabınızı Doğrulayın";
            
            string body = $@"
<div style='background: #0b1220; padding: 40px 0; font-family: sans-serif;'>

    <div style='max-width: 600px; margin: 0 auto; background: rgba(15,23,42,.92); border: 1px solid rgba(255,255,255,.08); border-radius: 24px; overflow: hidden; box-shadow: 0 20px 60px rgba(0,0,0,.5);'>

        <!-- HEADER -->
        <div style='text-align:center; padding: 35px 20px; background: linear-gradient(135deg,#2563eb,#1d4ed8);'>
            
            

            <h1 style='color:white; margin:0; font-size:24px; font-weight:800;'>
                Monobase
            </h1>

            <p style='color:rgba(255,255,255,.85); margin:6px 0 0 0; font-size:14px;'>
                Güvenli Hesap Doğrulama
            </p>

        </div>

        <!-- BODY -->
        <div style='padding:30px;'>

            <h2 style='color:#ffffff; margin-top:0;'>
                Hoş Geldiniz! 👋
            </h2>

            <p style='color:#94a3b8; line-height:1.6; font-size:14px;'>
                Monobase hesabınızı kullanmaya başlamak için e-posta adresinizi doğrulamanız gerekiyor.
                Bu işlem yalnızca birkaç saniye sürer.
            </p>

            <!-- BUTTON -->
            <div style='text-align:center; margin:30px 0;'>

                <a href='{confirmationLink}'
                   style='display:inline-block;
                          background:linear-gradient(135deg,#2563eb,#1d4ed8);
                          color:white;
                          padding:14px 26px;
                          border-radius:14px;
                          text-decoration:none;
                          font-weight:700;
                          box-shadow:0 10px 30px rgba(37,99,235,.3);'>
                    Hesabımı Doğrula
                </a>

            </div>

            <div style='height:1px;background:rgba(255,255,255,.08);margin:25px 0;'></div>

            <p style='font-size:12px; color:#64748b; line-height:1.6;'>
                Eğer bu hesabı siz oluşturmadıysanız bu e-postayı görmezden gelebilirsiniz.<br><br>

                Link çalışmıyorsa aşağıdaki adresi tarayıcınıza yapıştırın:<br>

                <span style='color:#60a5fa; word-break:break-all;'>
                    {confirmationLink}
                </span>
            </p>

        </div>

        <!-- FOOTER -->
        <div style='text-align:center; padding:18px; font-size:11px; color:#64748b; border-top:1px solid rgba(255,255,255,.06);'>
            © {DateTime.Now.Year} Monobase • Secure Database Platform
        </div>

    </div>

</div>";

            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}