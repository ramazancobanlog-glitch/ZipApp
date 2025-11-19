using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace login.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            // Prefer environment variable for design-time migrations so dotnet-ef
            // can connect to a remote database (useful for CI or remote DBs)
            var envConn = Environment.GetEnvironmentVariable("DefaultConnection")
                          ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            var conn = string.IsNullOrWhiteSpace(envConn)
                ? "Server=localhost;Port=3306;Database=ZipApp;User=root;Password=your_mysql_password;"
                : envConn;
            try
            {
                // Try AutoDetect (works when connection can be established)
                optionsBuilder.UseMySql(conn, ServerVersion.AutoDetect(conn));
            }
            catch
            {
                // If AutoDetect fails (e.g., remote DB blocked), fall back to a default MySQL Server version
                // This prevents EF design-time exceptions when the DB is inaccessible.
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 32));
                optionsBuilder.UseMySql(conn, serverVersion);
            }

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
