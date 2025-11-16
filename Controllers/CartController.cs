using login.Data;
using login.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using login.Hubs;
using login.Services;
using login.Helpers;
#nullable enable

namespace login.Controllers
{
	public class CartController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IyzipayService _iyzipayService;
		private readonly IHubContext<NotificationHub> _hub;
		private readonly EmailService _emailService;
		private readonly ILogger<CartController> _logger;

		public CartController(ApplicationDbContext context, IyzipayService iyzipayService, 
			IHubContext<NotificationHub> hub, EmailService emailService, ILogger<CartController> logger)
		{
			_context = context;
			_iyzipayService = iyzipayService;
			_hub = hub;
			_emailService = emailService;
			_logger = logger;
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

		[HttpPost]
		public IActionResult RemoveItem(int itemId)
		{
			var username = HttpContext.Session.GetString("Username");
			if (string.IsNullOrEmpty(username))
				return Json(new { success = false, redirect = Url.Action("Index", "Login") });

			var item = _context.CartItems.Include(i => i.Cart).FirstOrDefault(i => i.Id == itemId && i.Cart.Username == username && i.Cart.Status == CartStatus.Draft);
			if (item == null)
				return Json(new { success = false, message = "Ürün bulunamadı veya silinemiyor." });

			_context.CartItems.Remove(item);
			_context.SaveChanges();
			return Json(new { success = true });
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

		[HttpPost]
		public async Task<IActionResult> SubmitForApprovalAjax(int cartId)
		{
			var username = HttpContext.Session.GetString("Username");
			if (string.IsNullOrEmpty(username))
			{
				return Json(new { success = false, redirect = Url.Action("Index", "Login") });
			}

			var cart = _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.Id == cartId && c.Username == username);
			if (cart == null)
				return Json(new { success = false, message = "Sepet bulunamadı" });

			cart.Status = CartStatus.AwaitingApproval;
			_context.SaveChanges();

			// notify admin pages via SignalR
			try
			{
				await _hub.Clients.All.SendAsync("NewCartSubmitted", cart.Id);
			}
			catch { }

			return Json(new { success = true, message = "Sipariş yöneticinin onayına gönderildi." });
		}

		private string BuildOrderConfirmationEmail(Cart cart)
		{
			decimal total = 0;
			var itemDetails = new List<string>();

			if (cart.Items != null)
			{
				foreach (var item in cart.Items)
				{
					if (item.Product != null)
					{
						decimal itemTotal = item.Quantity * item.Product.Price;
						total += itemTotal;
						itemDetails.Add($"- {item.Product.Name}: {item.Quantity} adet x {TurkishLiraFormatting.Format(item.Product.Price)} = {TurkishLiraFormatting.Format(itemTotal)}");
					}
				}	
			}

			var items = string.Join("\n", itemDetails);

			return $@"
<html>
<body style='font-family: Arial, sans-serif;'>
	<h2>Siparişiniz Onaylandı</h2>
	<p>Sayın {cart.Username},</p>
	<p>Siparişiniz başarıyla onaylandı. Sipariş detaylarınız aşağıdadır:</p>
	
	<h3>Sipariş Detayları:</h3>
	<p>Sipariş Numarası: {cart.Id}</p>
	
	<div style='margin: 20px 0; padding: 10px; background-color: #f5f5f5;'>
		{items}
	</div>
	
	<h3>Toplam Tutar: {TurkishLiraFormatting.Format(total)}</h3>
	
	<p>Siparişiniz için teşekkür ederiz!</p>
	<p>Bizi tercih ettiğiniz için teşekkürler.</p>
</body>
</html>";
		}

		[AcceptVerbs("GET", "POST")]
		[IgnoreAntiforgeryToken]
		public async Task<IActionResult> PaymentResult(string token)
		{
			// Iyzico may POST the token back or redirect with GET; accept both.
			// If no token, check if we have TempData message (from redirect after payment)
			if (string.IsNullOrEmpty(token))
			{
				// try to read from form (POST) or query
				if (Request.HasFormContentType && Request.Form.ContainsKey("token"))
				{
					token = Request.Form["token"].ToString();
				}
				else if (Request.Query.ContainsKey("token"))
				{
					token = Request.Query["token"].ToString();
				}
			}

			// If still no token, just show the page with TempData message (handles redirect case)
			if (string.IsNullOrEmpty(token))
			{
				// TempData will be set by previous redirect; just render the view
				return View();
			}

			var result = _iyzipayService.RetrieveCheckoutForm(token);
			if (result != null && result.PaymentStatus == "SUCCESS")
			{
				// find cart using basket id
				if (int.TryParse(result.BasketId, out var cartId))
				{
					var cart = await _context.Carts
						.Include("Items.Product")
						.FirstOrDefaultAsync(c => c.Id == cartId);

					if (cart != null)
					{
						cart.Status = CartStatus.Confirmed;
						_context.SaveChanges();

						// Get user email
						var user = _context.Users.FirstOrDefault(u => u.Username == cart.Username);
						if (user?.Email != null)
						{
							try
							{
								// Build email content
								var emailBody = BuildOrderConfirmationEmail(cart);
								await _emailService.SendEmailAsync(
									user.Email,
									"Siparişiniz Onaylandı",
									emailBody
								);
							}
							catch (Exception ex)
							{
								// Log but don't fail the transaction
								_logger.LogError(ex, "Sipariş onay e-postası gönderilemedi: {Message}", ex.Message);
							}
						}
					}
				}

				TempData["PaymentResult"] = "Ödeme başarılı, siparişiniz onaylandı.";
				return RedirectToAction("PaymentResult", "Cart");
			}
			else
			{
				TempData["PaymentResult"] = "Ödeme başarısız veya iptal edildi.";
				return RedirectToAction("PaymentResult", "Cart");
			}
		}

		[HttpGet]
		public IActionResult Orders()
		{
			var username = HttpContext.Session.GetString("Username");
			if (string.IsNullOrEmpty(username))
				return RedirectToAction("Index", "Login");

			var orders = _context.Carts
				.Include(c => c.Items!)
				.ThenInclude(i => i.Product)
				.Where(c => c.Username == username && c.Status != CartStatus.Draft)
				.OrderByDescending(c => c.CreatedAt)
				.ToList();

			return View(orders);
		}
	}
}
