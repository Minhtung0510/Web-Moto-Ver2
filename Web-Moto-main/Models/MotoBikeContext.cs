using Microsoft.EntityFrameworkCore;

namespace MotoBikeStore.Models
{
    public class MotoBikeContext : DbContext
    {
        public MotoBikeContext(DbContextOptions<MotoBikeContext> options) : base(options) {}
        
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Coupon> Coupons => Set<Coupon>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Cấu hình quan hệ
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
    
    public static class DbSeeder
    {
        public static void Seed(MotoBikeContext db)
        {
            // Seed Categories
            if (!db.Categories.Any())
            {
                db.Categories.AddRange(
                    new Category { Name = "Xe Tay Ga", Description = "Xe số tự động, tiện lợi đô thị" },
                    new Category { Name = "Xe Số", Description = "Xe số côn, tiết kiệm nhiên liệu" },
                    new Category { Name = "Xe Phân Khối Lớn", Description = "Xe PKL, mạnh mẽ" },
                    new Category { Name = "Phụ Tùng", Description = "Phụ tùng chính hãng" }
                );
                db.SaveChanges();
            }
            
            // Seed Products
            if (!db.Products.Any())
            {
                var tayGa = db.Categories.First(c => c.Name == "Xe Tay Ga");
                var xeSo = db.Categories.First(c => c.Name == "Xe Số");
                
                db.Products.AddRange(
                    new Product { 
                        Name="Honda Air Blade 160", Brand="Honda", Engine="160cc", Fuel="5.5L",
                        Rating=4.8m, Price=48990000, OldPrice=57500000, DiscountPercent=15, 
                        ImageUrl="/images/airblade160.jpg", CategoryId=tayGa.Id,
                        Description="Thiết kế thể thao, động cơ eSP+", Stock=50,
                        Color="Đỏ, Đen, Xám", Warranty="3 năm"
                    },
                    new Product { 
                        Name="Yamaha Exciter 155 VVA", Brand="Yamaha", Engine="155cc", Fuel="5.0L",
                        Rating=4.9m, Price=52490000, Badge="new", ImageUrl="/images/exciter155.jpg",
                        CategoryId=xeSo.Id, Description="Công nghệ VVA, phanh ABS", Stock=30,
                        Color="Xanh GP, Đen, Đỏ", Warranty="3 năm"
                    },
                    new Product { 
                        Name="Honda Vision 2024", Brand="Honda", Engine="110cc", Fuel="5.2L",
                        Rating=4.7m, Price=32990000, ImageUrl="/images/vision2024.jpg",
                        CategoryId=tayGa.Id, Description="Sang trọng, tiết kiệm", Stock=80,
                        Color="Bạc, Đen, Nâu", Warranty="3 năm"
                    },
                    new Product { 
                        Name="Yamaha Janus Premium", Brand="Yamaha", Engine="125cc", Fuel="5.5L",
                        Rating=4.6m, Price=33500000, Badge="hot", ImageUrl="/images/janus.jpg",
                        CategoryId=tayGa.Id, Description="Thiết kế retro độc đáo", Stock=45,
                        Color="Xanh Mint, Vàng, Hồng", Warranty="3 năm"
                    }
                );
                db.SaveChanges();
            }
            
            // Seed Coupons
            if (!db.Coupons.Any())
            {
                db.Coupons.AddRange(
                    new Coupon { 
                        Code="WELCOME10", Description="Giảm 10% cho đơn đầu tiên",
                        DiscountPercent=10, MinOrderAmount=5000000, MaxDiscountAmount=2000000,
                        StartDate=DateTime.UtcNow, EndDate=DateTime.UtcNow.AddMonths(3),
                        UsageLimit=100, IsActive=true
                    },
                    new Coupon { 
                        Code="SUMMER2024", Description="Khuyến mãi hè - Giảm 1 triệu",
                        DiscountAmount=1000000, MinOrderAmount=30000000,
                        StartDate=DateTime.UtcNow, EndDate=DateTime.UtcNow.AddMonths(2),
                        UsageLimit=50, IsActive=true
                    },
                    new Coupon { 
                        Code="VIP15", Description="Giảm 15% cho khách VIP",
                        DiscountPercent=15, MinOrderAmount=10000000, MaxDiscountAmount=5000000,
                        StartDate=DateTime.UtcNow, EndDate=DateTime.UtcNow.AddYears(1),
                        UsageLimit=0, IsActive=true
                    }
                );
                db.SaveChanges();
            }
            
            // Seed Admin User
            if (!db.Users.Any())
            {
                db.Users.Add(new User
                {
                    Email = "admin@motobike.vn",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "Administrator",
                    Phone = "0901234567",
                    Role = "Admin",
                    IsActive = true
                });
                db.SaveChanges();
            }
        }
    }
}