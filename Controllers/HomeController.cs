using System.Diagnostics;
using login.Models;
using login.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace login.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _context.Products
                .Include(p => p.Category)
                .ToList();
            var categories = _context.Categories.ToList();
            string GetIp(HttpContext context)
            {
                var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (string.IsNullOrEmpty(ip))
                {
                    ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                }
                return ip;
            }
            
            var userIp = GetIp(HttpContext);
            Console.WriteLine($"Kullanýcý IP Adresi: {userIp}");

            var vm = new HomeIndexViewModel
            {
                Products = products,
                Categories = categories
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
