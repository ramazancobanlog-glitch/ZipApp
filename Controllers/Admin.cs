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

            // build pending and confirmed lists with item details
            var pendingCarts = _context.Carts
                .Where(c => c.Status == Models.CartStatus.AwaitingApproval)
                .Include(c => c.Items!)
                .ThenInclude(i => i.Product)
                .ToList();

            var confirmedCarts = _context.Carts
                .Where(c => c.Status == Models.CartStatus.Confirmed)
                .Include(c => c.Items!)
                .ThenInclude(i => i.Product)
                .ToList();

            List<Models.AdminCartViewModel> pendingVm = new();
            List<Models.AdminCartViewModel> confirmedVm = new();

            foreach (var c in pendingCarts)
            {
                var user = _context.Users.FirstOrDefault(u => u.Username == c.Username);
                var vm = new Models.AdminCartViewModel
                {
                    Id = c.Id,
                    Username = c.Username,
                    Email = user?.Email,
                    CreatedAt = c.CreatedAt,
                    Status = c.Status
                };
                foreach (var it in c.Items ?? Enumerable.Empty<Models.CartItem>())
                {
                    vm.Items.Add(new Models.AdminCartItemViewModel
                    {
                        ProductName = it.Product?.Name,
                        Quantity = it.Quantity,
                        Price = it.Product?.Price ?? 0
                    });
                }
                pendingVm.Add(vm);
            }

            foreach (var c in confirmedCarts)
            {
                var user = _context.Users.FirstOrDefault(u => u.Username == c.Username);
                var vm = new Models.AdminCartViewModel
                {
                    Id = c.Id,
                    Username = c.Username,
                    Email = user?.Email,
                    CreatedAt = c.CreatedAt,
                    Status = c.Status
                };
                foreach (var it in c.Items ?? Enumerable.Empty<Models.CartItem>())
                {
                    vm.Items.Add(new Models.AdminCartItemViewModel
                    {
                        ProductName = it.Product?.Name,
                        Quantity = it.Quantity,
                        Price = it.Product?.Price ?? 0
                    });
                }
                confirmedVm.Add(vm);
            }

            var model = new Models.AdminIndexViewModel
            {
                Pending = pendingVm,
                Confirmed = confirmedVm
            };

            return View(model);
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
