using Microsoft.AspNetCore.Mvc;
using login.Data;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace login.ViewComponents
{
    public class CartBadgeViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _ctx;

        public CartBadgeViewComponent(ApplicationDbContext db, IHttpContextAccessor ctx)
        {
            _db = db;
            _ctx = ctx;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var username = _ctx.HttpContext?.Session.GetString("Username");
            var count = 0;
            if (!string.IsNullOrEmpty(username))
            {
                count = _db.Carts.Where(c => c.Username == username && c.Status == Models.CartStatus.Draft)
                    .SelectMany(c => c.Items!)
                    .Sum(i => (int?)i.Quantity) ?? 0;
            }

            return View(count);
        }
    }
}
