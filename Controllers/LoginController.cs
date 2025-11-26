using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading.Tasks;
using login.Data;
using login.Models;
using login.Services;
using Microsoft.AspNetCore.Mvc;

namespace login.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public LoginController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    public IActionResult Profile()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {

            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
            if (user == null)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }

            if (!user.IsEmailConfirmed)
            {
                ViewBag.Error = "Lütfen önce email adresinizi doğrulayın.";
                return View();
            }
            if(user.IsAdmin)
            {
                // Admin kullanıcılar için doğrudan oturum açma
                HttpContext.Session.SetString("UserEmail", user.Email ?? "");
           
                HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());
                return RedirectToAction("Index", "Admin");
            }
            var verficationCode = new Random().Next(100000, 999999).ToString();
            user.verificationCode = verficationCode;
            user.VerificationCodeExpiresAt = DateTime.Now.AddMinutes(10);
            _context.SaveChanges();
                if (string.IsNullOrEmpty(user.Email))
                {
                    ViewBag.Error = "Email adresi bulunamadı.";
                    return View();
                }
                
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Giriş Doğrulama Kodu",
                    $"Giriş yapmak için doğrulama kodunuz: <b>{verficationCode}</b>. Bu kod 10 dakika içinde geçersiz olacaktır."
                );

                TempData["UserEmail"] = user.Email;
            return RedirectToAction("VerifyCode");
                
        }

        [HttpGet]
        public IActionResult VerifyCode()
        {
            
         
            
            var email = TempData["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index");
            }
            ViewBag.MaskedEmail = maskemail(email);
            ViewBag.Email = email;
            // TempData'yı korumak için yeniden atıyoruz
            TempData["UserEmail"] = email;
            return View("verify");
            
        }
             private string maskemail(string email){
                var parts= email.Split('@');
                string name = parts[0];
                string domain = parts[1];
                string first3=name.Length<=3 ? name : name.Substring(0,3);
                  return first3+"****@"+domain;
                  
             }
        [HttpPost]
        public IActionResult VerifyCode(string email, string code)
        {
            
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            // Kodun süresi dolmuş mu kontrol et
            if (user.VerificationCodeExpiresAt < DateTime.Now)
            {
                TempData["Error"] = "Doğrulama kodunun süresi dolmuş.";
                return RedirectToAction("Index");
            }

            // Kod doğru mu kontrol et
            if (user.verificationCode == code)
            {
                // Kodu temizle
                user.verificationCode = null;
                user.VerificationCodeExpiresAt = null;
                _context.SaveChanges();

                // Session'ları ayarla
                if (!string.IsNullOrEmpty(user.Email))
                    HttpContext.Session.SetString("UserEmail", user.Email);
                if (!string.IsNullOrEmpty(user.Username))
                    HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

 //dsadasdasasdasdsa
                // Admin ise admin paneline, değilse ana sayfaya yönlendir
                if (user.IsAdmin)
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
           
            }

            TempData["Error"] = "Geçersiz doğrulama kodu.";
            TempData["UserEmail"] = email; // Email'i geri gönder
            return View("verify");
            

        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string email)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "Bu kullanıcı adı zaten alınmış.";
                return View();
            }

            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "Bu email adresi zaten kullanılıyor.";
                return View();
            }

            var token = Guid.NewGuid().ToString();
            var user = new User
            {
                Username = username,
                Password = password,
                Email = email,
                EmailConfirmationToken = token,
                IsEmailConfirmed = false,
                IsAdmin = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var confirmationLink = Url.Action("ConfirmEmail", "Login",
                new { userId = user.Id, token = token },
                Request.Scheme);

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Email Doğrulama",
                    $"Hesabınızı doğrulamak için tıklayın: <a href='{confirmationLink}'>Email Doğrula</a>"
                );

                ViewBag.Message = "Kayıt başarılı! Lütfen emailinizi doğrulayın.";
                return View();
            }
            catch (Exception)
            {
                ViewBag.Error = "Email gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(int userId, string token)
        {
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null || user.EmailConfirmationToken != token)
            {
                return View("Error", new ErrorViewModel { RequestId = "Invalid confirmation link" });
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null; // Token'ı temizle
            await _context.SaveChangesAsync();

            ViewBag.Message = "Email adresiniz başarıyla doğrulandı. Şimdi giriş yapabilirsiniz.";
            return RedirectToAction("Index");
        }

        public IActionResult Logout()
        {
            Debug.WriteLine("Logging out user: " + HttpContext.Session.GetString("Username"));
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}
        
    