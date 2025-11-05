using login.Models;
using Microsoft.EntityFrameworkCore;

namespace login.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "1234",
                    IsEmailConfirmed = true,
                    IsAdmin = true
                }
            );
            // Seed some example products
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Ürün A", Price = 49.90M },
                new Product { Id = 2, Name = "Ürün B", Price = 79.50M },
                new Product { Id = 3, Name = "Ürün C", Price = 19.00M }
            );
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
    }
}
