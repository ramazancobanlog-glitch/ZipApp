using System.Globalization;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Logging;

namespace login.Services
{
    public class IyzipayService
    {
        private readonly Options _options;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IyzipayService> _logger;

        public IyzipayService(IConfiguration configuration, ILogger<IyzipayService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var apiKey = configuration["Iyzico:ApiKey"];
            var secretKey = configuration["Iyzico:SecretKey"];
            var baseUrl = configuration["Iyzico:BaseUrl"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("Iyzico configuration missing. ApiKey set: {hasApi}, SecretKey set: {hasSecret}, BaseUrl set: {hasBase}",
                    !string.IsNullOrEmpty(apiKey), !string.IsNullOrEmpty(secretKey), !string.IsNullOrEmpty(baseUrl));
            }

            _options = new Options
            {
                ApiKey = apiKey,
                SecretKey = secretKey,
                BaseUrl = baseUrl
            };
        }

        public string InitializeCheckout(login.Models.Cart cart, string callbackUrl, string returnUrl, login.Models.User? buyerUser)
        {
            try
            {
                var total = cart.Items?.Sum(i => (i.Product?.Price ?? 0M) * i.Quantity) ?? 0M;

                var request = new CreateCheckoutFormInitializeRequest
                {
                    Locale = "tr",
                    Price = total.ToString("F2", CultureInfo.InvariantCulture),
                    PaidPrice = total.ToString("F2", CultureInfo.InvariantCulture),
                    Currency = Currency.TRY.ToString(),
                    BasketId = cart.Id.ToString(),
                    PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                    CallbackUrl = callbackUrl
                };

                // Buyer
                request.Buyer = new Buyer
                {
                    Id = buyerUser?.Id.ToString() ?? "0",
                    Name = buyerUser?.Username ?? "Müşteri",
                    City = "Bursa",
                    Country = "Türkiye",
                    Surname = "Müşteri",
                    GsmNumber = "",
                    Email = buyerUser?.Email ?? "",
                    IdentityNumber = "12345678901",
                    RegistrationAddress = "çirişhanemah",
                    Ip = "127.0.0.1"
                };

                // Addresses
                request.ShippingAddress = new Address
                {
                    ContactName = buyerUser?.Username ?? "Müşteri",
                    City = "Bursa",
                    Country = "Türkiye",
                    Description = "asdas",
                };

                request.BillingAddress = new Address
                {
                    ContactName = buyerUser?.Username ?? "Müşteri",
                    City = "Bursa",
                    Country = "Türkiye",
                    Description = "adasdas",
                };

                // Basket items
                var items = new List<BasketItem>();
                if (cart.Items != null)
                {
                    foreach (var it in cart.Items)
                    {
                        items.Add(new BasketItem
                        {
                            Id = it.ProductId.ToString(),
                            Name = it.Product?.Name ?? "Ürün",
                            Category1 = "Genel",
                            ItemType = BasketItemType.PHYSICAL.ToString(),
                            Price = ((it.Product?.Price ?? 0M) * it.Quantity).ToString("F2", CultureInfo.InvariantCulture)
                        });
                    }
                }

                request.BasketItems = items;

                var checkoutFormInitialize = CheckoutFormInitialize.Create(request, _options);

                if (checkoutFormInitialize == null)
                {
                    _logger.LogWarning("Iyzipay returned null for CheckoutFormInitialize.");
                    return "<div>Ödeme başlatılırken hata oluştu. Lütfen API anahtarlarını ve konfigürasyonu kontrol edin. (checkoutForm empty)</div>";
                }

                var content = checkoutFormInitialize.CheckoutFormContent;
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Iyzipay CheckoutFormContent is empty. Response status: {status}", checkoutFormInitialize.Status);
                    return $"<div>Ödeme başlatılamadı. Iyzipay yanıtı boş. Status: {checkoutFormInitialize.Status} - Hata: {checkoutFormInitialize.ErrorMessage}</div>";
                }

                return content ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while initializing Iyzipay checkout");
                return $"<div>Ödeme başlatılırken beklenmeyen hata oluştu: {ex.Message}</div>";
            }
        }

        public CheckoutForm RetrieveCheckoutForm(string token)
        {
            var req = new RetrieveCheckoutFormRequest { Token = token };
            var checkoutForm = CheckoutForm.Retrieve(req, _options);
            return checkoutForm;
        }
    }
}
