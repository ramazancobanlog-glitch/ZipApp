using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace login.Services
{
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _password;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _logger = logger;
            
            _smtpHost = configuration["Email:SmtpHost"];
            if (string.IsNullOrEmpty(_smtpHost))
            {
                throw new ArgumentException("SMTP host ayarı bulunamadı. Lütfen appsettings.json dosyasını kontrol edin.");
            }

            if (!int.TryParse(configuration["Email:SmtpPort"], out _smtpPort))
            {
                throw new ArgumentException("SMTP port ayarı geçersiz. Lütfen appsettings.json dosyasını kontrol edin.");
            }

            _fromEmail = configuration["Email:FromAddress"];
            if (string.IsNullOrEmpty(_fromEmail))
            {
                throw new ArgumentException("Gönderici email adresi ayarı bulunamadı. Lütfen appsettings.json dosyasını kontrol edin.");
            }

            _password = configuration["Email:Password"];
            if (string.IsNullOrEmpty(_password))
            {
                throw new ArgumentException("Email şifre ayarı bulunamadı. Lütfen appsettings.json dosyasını kontrol edin.");
            }
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_fromEmail);
                message.Subject = subject;
                message.To.Add(new MailAddress(to));
                message.Body = body;
                message.IsBodyHtml = true;

                using var smtpClient = new SmtpClient(_smtpHost)
                {
                    Port = _smtpPort,
                    Credentials = new NetworkCredential(_fromEmail, _password),
                    EnableSsl = true,
                    Timeout = 20000 // 20 saniye timeout
                };

                await smtpClient.SendMailAsync(message);
                _logger.LogInformation($"Email başarıyla gönderildi: {to}");
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, $"SMTP hatası oluştu: {ex.Message}, Status Code: {ex.StatusCode}");
                throw new Exception($"Email gönderilirken SMTP hatası oluştu: {GetSmtpErrorMessage(ex)}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Email gönderilirken beklenmeyen hata: {ex.Message}");
                throw new Exception("Email gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.", ex);
            }
        }

        private string GetSmtpErrorMessage(SmtpException ex)
        {
            return ex.StatusCode switch
            {
                SmtpStatusCode.MailboxBusy => "Email sunucusu şu anda meşgul. Lütfen daha sonra tekrar deneyin.",
                SmtpStatusCode.MailboxUnavailable => "Email adresi geçersiz veya kullanılamıyor.",
                SmtpStatusCode.ClientNotPermitted => "Email gönderme izniniz yok. Lütfen kimlik doğrulama bilgilerini kontrol edin.",
                _ => "Email gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin."
            };
        }
    }
}