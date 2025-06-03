using Microsoft.EntityFrameworkCore;
using MarketPlaceApi.Models;

namespace MarketPlaceApi.Data
{
    public class MarketPlaceDbContext : DbContext
    {
        public MarketPlaceDbContext(DbContextOptions<MarketPlaceDbContext> options) : base(options) { }

        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<SavedProduct> SavedProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // إعداد الـ Primary Keys
            modelBuilder.Entity<Vendor>()
                .HasKey(v => v.PhoneNumber);

            modelBuilder.Entity<Customer>()
                .HasKey(c => c.PhoneNumber);

            // إعداد الـ Composite Key لـ SavedProduct
            modelBuilder.Entity<SavedProduct>()
                .HasKey(sp => new { sp.CustomerPhoneNumber, sp.ProductId });

            // إعداد العلاقات مع توضيح الـ Foreign Keys بشكل صريح

            // العلاقة بين Product و Vendor
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Vendor) // Product ليه Vendor واحد
                .WithMany(v => v.Products) // Vendor عنده كذا Product
                .HasForeignKey(p => p.VendorPhoneNumber); // الـ Foreign Key هو VendorPhoneNumber

            // العلاقة بين CartItem و Customer
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Customer) // CartItem ليه Customer واحد
                .WithMany(c => c.CartItems) // Customer عنده كذا CartItem
                .HasForeignKey(ci => ci.CustomerPhoneNumber); // الـ Foreign Key هو CustomerPhoneNumber

            // العلاقة بين CartItem و Product
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product) // CartItem ليه Product واحد
                .WithMany(p => p.CartItems) // Product عنده كذا CartItem
                .HasForeignKey(ci => ci.ProductId); // الـ Foreign Key هو ProductId

            // العلاقة بين Order و Customer
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer) // Order ليه Customer واحد
                .WithMany(c => c.Orders) // Customer عنده كذا Order
                .HasForeignKey(o => o.CustomerPhoneNumber); // الـ Foreign Key هو CustomerPhoneNumber

            // العلاقة بين OrderItem و Order
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order) // OrderItem ليه Order واحد
                .WithMany(o => o.OrderItems) // Order عنده كذا OrderItem
                .HasForeignKey(oi => oi.OrderId); // الـ Foreign Key هو OrderId

            // العلاقة بين OrderItem و Product
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product) // OrderItem ليه Product واحد
                .WithMany(p => p.OrderItems) // Product عنده كذا OrderItem
                .HasForeignKey(oi => oi.ProductId); // الـ Foreign Key هو ProductId

            // العلاقة بين SavedProduct و Customer
            modelBuilder.Entity<SavedProduct>()
                .HasOne(sp => sp.Customer) // SavedProduct ليه Customer واحد
                .WithMany(c => c.SavedProducts) // Customer عنده كذا SavedProduct
                .HasForeignKey(sp => sp.CustomerPhoneNumber); // الـ Foreign Key هو CustomerPhoneNumber

            // العلاقة بين SavedProduct و Product
            modelBuilder.Entity<SavedProduct>()
                .HasOne(sp => sp.Product) // SavedProduct ليه Product واحد
                .WithMany(p => p.SavedProducts) // Product عنده كذا SavedProduct
                .HasForeignKey(sp => sp.ProductId); // الـ Foreign Key هو ProductId

            // إضافة Precision و Scale للخصائص من نوع decimal
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // إضافة الكونفيج بتاع الحقول الجديدة في Order
            modelBuilder.Entity<Order>()
                .Property(o => o.PhoneNumber)
                .IsRequired();

            modelBuilder.Entity<Order>()
                .Property(o => o.Comment)
                .IsRequired(false);
        }
    }
}