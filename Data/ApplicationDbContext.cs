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
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Elektronik", Slug = "elektronik" },
                new Category { Id = 2, Name = "Bilgisayar", Slug = "bilgisayar" },
                new Category { Id = 3, Name = "Ev & Yaşam", Slug = "ev-yasam" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Kablolu Mouse", Price = 49.90M, ImageUrl = "~/img/mouse.jpg", CategoryId = 1 },
                new Product { Id = 2, Name = "Mekanik Klavye", Price = 349.50M, ImageUrl = "~/img/klavye.jpg", CategoryId = 2 },
                new Product { Id = 3, Name = "Bardak Seti", Price = 99.00M, ImageUrl = "~/img/bardak.jpg", CategoryId = 3 },
                new Product { Id = 4, Name = "Bluetooth Kulaklık", Price = 199.00M, ImageUrl = "~/img/images.png", CategoryId = 1 },
                new Product { Id = 5, Name = "Laptop Soğutucu", Price = 269.00M, ImageUrl = "~/img/images.png", CategoryId = 2 },
                new Product { Id = 6, Name = "Dekoratif Yastık", Price = 59.90M, ImageUrl = "~/img/images.png", CategoryId = 3 },
                new Product { Id = 7, Name = "Gaming Mousepad", Price = 89.90M, ImageUrl = "~/img/images.png", CategoryId = 2 },
                new Product { Id = 8, Name = "Kahve Makinesi", Price = 699.90M, ImageUrl = "~/img/images.png", CategoryId = 3 }
            );
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Comment> Comments { get; set; }
    }
}
