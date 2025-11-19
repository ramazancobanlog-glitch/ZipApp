using login.Data;
using login.Services;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// add SignalR services for real-time notifications
builder.Services.AddSignalR();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // prefer environment variable for connection string (set by Render or local environment)
    var connectionString = Environment.GetEnvironmentVariable("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? builder.Configuration.GetConnectionString("DefaultConnection");
    // Use Pomelo MySQL EF Core provider
    try
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
    catch
    {
        // If AutoDetect fails (for example when remote DB is blocked during development),
        // use a default server version as a fallback so the app can still start for local dev.
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 32));
        options.UseMySql(connectionString, serverVersion);
    }
});

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
