using login.Data;
using login.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace login.Controllers
{
	public class CartController : Controller
	{
		private readonly ApplicationDbContext _context;

		public CartController(ApplicationDbContext context)
		{
			_context = context;
		}

		// POST: /Cart/AddToCart
		[HttpPost]
		public IActionResult AddToCart(int productId)
		{
			var username = HttpContext.Session.GetString("Username");
			if (string.IsNullOrEmpty(username))
				return RedirectToAction("Index", "Login");

			var product = _context.Products.Find(productId);
			if (product == null)
				return NotFound();

			var cart = _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.Username == username && c.Status == CartStatus.Draft);
			if (cart == null)
			{
				cart = new Cart { Username = username };
				_context.Carts.Add(cart);
				_context.SaveChanges();
				// reload including items
				cart = _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.Id == cart.Id)!;
			}

			var existing = cart.Items?.FirstOrDefault(i => i.ProductId == productId);
			if (existing != null)
			{
				existing.Quantity += 1;
			}
			else
			{
				var item = new CartItem { CartId = cart.Id, ProductId = productId, Quantity = 1 };
				_context.CartItems.Add(item);
			}

			_context.SaveChanges();

			return RedirectToAction("Index", "Home");
		}

		// Support legacy/mistyped URL: GET /Cart/AddtoCard -> redirect to home
		[HttpGet]
		public IActionResult AddtoCard()
		{
			return RedirectToAction("Index", "Home");
		}

		// Support legacy/mistyped URL: POST /Cart/AddtoCard (alias to AddToCart)
		[HttpPost]
		[ActionName("AddtoCard")]
		public IActionResult AddtoCardPost(int productId)
		{
			return AddToCart(productId);
		}

		[HttpGet]
		public IActionResult Index()
		{
			var username = HttpContext.Session.GetString("Username");
			if (string.IsNullOrEmpty(username))
				return RedirectToAction("Index", "Login");

			var cart = _context.Carts
				.Include(c => c.Items!)
				.ThenInclude(i => i.Product)
				.FirstOrDefault(c => c.Username == username && c.Status == CartStatus.Draft);

			return View(cart);
		}

		[HttpPost]
		public IActionResult ConfirmCart(int cartId)
		{
			var username = HttpContext.Session.GetString("Username");
			if (string.IsNullOrEmpty(username))
				return RedirectToAction("Index", "Login");

			var cart = _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.Id == cartId && c.Username == username);
			if (cart == null)
				return NotFound();

			cart.Status = CartStatus.AwaitingApproval;
			_context.SaveChanges();

			TempData["CartSubmitted"] = "Siparişiniz yönetici onayına gönderildi.";

			return RedirectToAction("Index", "Home");
		}
	}
}
