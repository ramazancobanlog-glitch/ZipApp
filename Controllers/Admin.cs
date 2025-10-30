using Microsoft.AspNetCore.Mvc;
using login.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using login.Hubs;

namespace login.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public AdminController(ApplicationDbContext context, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "True")
                return RedirectToAction("Index", "Login");

            // show carts waiting for approval
            var pending = _context.Carts
                .Where(c => c.Status == Models.CartStatus.AwaitingApproval)
                .Include(c => c.Items!)
                .ThenInclude(i => i.Product)
                .ToList();

            return View(pending);
        }

        [HttpPost]
        public IActionResult ApproveCart(int id)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "True")
                return RedirectToAction("Index", "Login");

            var cart = _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.Id == id);
            if (cart == null) return NotFound();

            cart.Status = Models.CartStatus.Confirmed;
            _context.SaveChanges();

            // send real-time notification to clients that cart was approved
            try
            {
                _hub.Clients.All.SendAsync("CartApproved", id);
            }
            catch
            {
                // swallow hub errors so approval still works
            }

            return RedirectToAction("Index");
        }
    }
}
