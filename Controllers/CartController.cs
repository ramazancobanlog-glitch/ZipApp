using login.Data;
using login.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using login.Hubs;

namespace login.Controllers
{
	public class CartController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly login.Services.IyzipayService _iyzipayService;
		private readonly IHubContext<NotificationHub> _hub;


		public CartController(ApplicationDbContext context, login.Services.IyzipayService iyzipayService, IHubContext<NotificationHub> hub)
		{
			_context = context;
			_iyzipayService = iyzipayService;
			_hub = hub;
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

			// If request is AJAX (fetch), return JSON so client can stay on page
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.ContentType == "application/json")
			{
				var cartReload = _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.Username == username && c.Status == CartStatus.Draft);
				var itemCount = cartReload?.Items?.Sum(i => i.Quantity) ?? 0;
				return Json(new { success = true, count = itemCount });
			}

			return RedirectToAction("Index", "Home");
		}

		[HttpPost]
		public IActionResult AddToCartAjax([FromForm] int productId)
		{
			// convenience alias for AJAX calls; keep same behavior as AddToCart but returns JSON
			return AddToCart(productId);
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

			var cart = _context.Carts
				.Include(c => c.Items!)
				.ThenInclude(i => i.Product)
				.FirstOrDefault(c => c.Id == cartId && c.Username == username);
			if (cart == null)
				return NotFound();

			// prepare buyer info
			var user = _context.Users.FirstOrDefault(u => u.Username == username);

			// Build callback URL (iyzico will post token back here)
			var callbackUrl = Url.Action("PaymentResult", "Cart", null, Request.Scheme)!;

			// Initialize checkout and return embedded form
			var checkoutForm = _iyzipayService.InitializeCheckout(cart, callbackUrl, callbackUrl, user);

			// render a view that contains the Iyzipay checkout form
			return View("Payment", checkoutForm);
		}

		[HttpGet]
		public IActionResult PaymentResult(string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				TempData["PaymentResult"] = "Ödeme bilgisi alınamadı.";
				return RedirectToAction("Index", "Home");
			}

			var result = _iyzipayService.RetrieveCheckoutForm(token);
			if (result != null && result.PaymentStatus == "SUCCESS")
			{
				// find cart using basket id
				if (int.TryParse(result.BasketId, out var cartId))
				{
					var cart = _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.Id == cartId);
					if (cart != null)
					{
						cart.Status = CartStatus.Confirmed;
						_context.SaveChanges();
					}
				}

				TempData["PaymentResult"] = "Ödeme başarılı, siparişiniz onaylandı.";
			}
			else
			{
				TempData["PaymentResult"] = "Ödeme başarısız veya iptal edildi.";
			}

			return RedirectToAction("Index", "Home");
		}
	}
}
