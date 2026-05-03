using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using trendaura.Models;

namespace trendaura.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<MobileAccessory> MobileAccessories { get; set; } = null!;
        public DbSet<AccessoryReview> AccessoryReviews { get; set; } = null!;
        public DbSet<Wishlist> Wishlists { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Ensure explicit table names
            builder.Entity<Product>().ToTable("Products");
            builder.Entity<Category>().ToTable("Categories");
            builder.Entity<MobileAccessory>().ToTable("MobileAccessories");
            builder.Entity<AccessoryReview>().ToTable("AccessoryReviews");
            builder.Entity<Order>().ToTable("Orders");
            builder.Entity<OrderItem>().ToTable("OrderItems");
            builder.Entity<CartItem>().ToTable("CartItems");
            builder.Entity<Wishlist>().ToTable("Wishlists");
            builder.Entity<Review>().ToTable("Reviews");

            // Configure decimal precision to avoid warnings and potential data loss
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            // MobileAccessory price already configured via [Column] attribute

            // Configure CartItem relationships - NO ACTION to avoid cascade cycles
            builder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<CartItem>()
                .HasOne(c => c.MobileAccessory)
                .WithMany()
                .HasForeignKey(c => c.MobileAccessoryId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Wishlist relationships - NO ACTION to avoid cascade cycles
            builder.Entity<Wishlist>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Wishlist>()
                .HasOne(w => w.MobileAccessory)
                .WithMany()
                .HasForeignKey(w => w.MobileAccessoryId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure AccessoryReview relationships
            builder.Entity<AccessoryReview>()
                .HasOne(ar => ar.Accessory)
                .WithMany(a => a.Reviews)
                .HasForeignKey(ar => ar.AccessoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AccessoryReview>()
                .HasOne(ar => ar.User)
                .WithMany()
                .HasForeignKey(ar => ar.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}    