using login.Data;
using login.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// add SignalR services for real-time notifications
builder.Services.AddSignalR();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Email Service
builder.Services.AddSingleton<EmailService>();
// Configure Iyzico (Iyzipay) service
builder.Services.AddSingleton<IyzipayService>();
// Make HttpContext available to ViewComponents and services
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// map SignalR hub endpoint
app.MapHub<login.Hubs.NotificationHub>("/hubs/notifications");

app.Run();
