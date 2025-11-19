using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace login.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Ortam değişkeni varsa kullan, yoksa default LocalDB connection string
            var envConn = Environment.GetEnvironmentVariable("DefaultConnection")
                          ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            var conn = string.IsNullOrWhiteSpace(envConn)
                ? "Server=(localdb)\\MSSQLLocalDB;Database=LoginDB;Trusted_Connection=True;TrustServerCertificate=True;"
                : envConn;

            optionsBuilder.UseSqlServer(conn, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(); // opsiyonel ama önerilir
            });

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
