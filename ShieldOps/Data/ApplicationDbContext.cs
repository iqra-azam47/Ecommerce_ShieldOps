using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShieldOps.Models;

namespace ShieldOps.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<VerificationToken> VerificationTokens { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Cascade delete configurations & precision settings
            builder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Data Seeding for Core Categories
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Hardware Security Tokens", Description = "Physical cryptographic keys and hardware multi-factor authentication devices." },
                new Category { Id = 2, Name = "Encryption Software", Description = "Digital suites for securing file systems, communications, and database backends." }
            );

            // Data Seeding for Identity Security Roles (Using hardcoded, static GUIDs)
            string adminRoleId = "a1111111-1111-1111-1111-111111111111";
            string customerRoleId = "c2222222-2222-2222-2222-222222222222";

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = customerRoleId, Name = "Customer", NormalizedName = "CUSTOMER" }
            );

            // Data Seeding for default system Enterprise Admin profile (Using hardcoded, static GUIDs)
            string adminUserId = "00000000-0000-0000-0000-000000000000";
            var adminUser = new ApplicationUser
            {
                Id = adminUserId,
                UserName = "admin@shieldops.com",
                NormalizedUserName = "ADMIN@SHIELDOPS.COM",
                Email = "admin@shieldops.com",
                NormalizedEmail = "ADMIN@SHIELDOPS.COM",
                EmailConfirmed = true,
                FullName = "System Administrator",
                IsVerified = true,
                SecurityStamp = "STATIC_SECURITY_STAMP_VALUE"
            };

            // Password hashing implementation for seed user
            var passwordHasher = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "ShieldOpsSecure2026!");

            builder.Entity<ApplicationUser>().HasData(adminUser);

            // Mapping Admin user to Admin Role
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { UserId = adminUserId, RoleId = adminRoleId }
            );

            // Data Seeding for Cyber Security Products
            builder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Titanium MFA Security Key",
                    Description = "Military-grade physical USB-C authentication key with biometrics.",
                    Price = 85.00m,
                    StockQuantity = 15,
                    Type = ProductType.Physical,
                    ImageUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=500",
                    CategoryId = 1
                },
                new Product
                {
                    Id = 2,
                    Name = "CryptoShield Enterprise Edition",
                    Description = "Next-gen localized AES-256 software suite for full-disk database encryption.",
                    Price = 299.99m,
                    StockQuantity = 999,
                    Type = ProductType.Digital,
                    ImageUrl = "https://images.unsplash.com/photo-1563986768609-322da13575f3?w=500",
                    CategoryId = 2
                },
                new Product
                {
                    Id = 3,
                    Name = "YubiArmor Pro Token",
                    Description = "NFC-enabled physical hardware token designed for zero-trust architectures.",
                    Price = 45.50m,
                    StockQuantity = 3,
                    Type = ProductType.Physical,
                    ImageUrl = "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=500",
                    CategoryId = 1
                }
            );

            // ==========================================
            // FIX: AUTOMATIC SQLITE TYPE CONVERTER BLOCK
            // ==========================================
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                foreach (var entityType in builder.Model.GetEntityTypes())
                {
                    var properties = entityType.GetProperties()
                        .Where(p => p.ClrType == typeof(string));

                    foreach (var property in properties)
                    {
                        // SQL Server types (nvarchar(max), nvarchar(450)) ko SQLite standard "TEXT" par badalna
                        var columnNameType = property.GetColumnType();
                        if (string.IsNullOrEmpty(columnNameType) || columnNameType.Contains("nvarchar"))
                        {
                            property.SetColumnType("TEXT");
                        }
                    }
                }
            }
        }
    }
}