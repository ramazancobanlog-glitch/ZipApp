using login.Data;
using login.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// add SignalR services for real-time notifications
builder.Services.AddSignalR();

// LocalDB connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(); // opsiyonel ama tavsiye edilir
    })
);

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Authentication (Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";    // giriş yapılmadığında yönlendirilecek sayfa
        options.LogoutPath = "/Login/Logout";  // çıkış yapıldığında yönlendirilecek sayfa
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Configure Email Service
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<IyzipayService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication(); // <- BUNU EKLEDİK
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// map SignalR hub endpoint
app.MapHub<login.Hubs.NotificationHub>("/hubs/notifications");

app.Run();
