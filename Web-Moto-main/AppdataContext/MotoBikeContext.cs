using Microsoft.EntityFrameworkCore;
using MotoBikeStore.Services;

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
        public DbSet<SeasonalPromotion> SeasonalPromotions => Set<SeasonalPromotion>();
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
            
            if (!db.Products.Any())
{
    var tayGa  = db.Categories.First(c => c.Name == "Xe Tay Ga");
    var xeSo   = db.Categories.First(c => c.Name == "Xe Số");
    var phuTung = db.Categories.First(c => c.Name == "Phụ Tùng"); // ✅ Thêm dòng này

    db.Products.AddRange(
        // ── Xe hiện có ──
        new Product { 
            Name="Honda Air Blade 160", Brand="Honda", Engine="160cc", Fuel="5.5L",
            Rating=4.8m, Price=48990000, OldPrice=57500000, DiscountPercent=15, 
            ImageUrl="/images/AirBlade160.jpg", CategoryId=tayGa.Id,
            Description="Thiết kế thể thao, động cơ eSP+", Stock=50,
            Color="Đỏ, Đen, Xám", Warranty="3 năm"
        },
        new Product { 
            Name="Yamaha Exciter 155 VVA", Brand="Yamaha", Engine="155cc", Fuel="5.0L",
            Rating=4.9m, Price=52490000, Badge="new", ImageUrl="/images/Ex155VVA.jpg",
            CategoryId=xeSo.Id, Description="Công nghệ VVA, phanh ABS", Stock=30,
            Color="Xanh GP, Đen, Đỏ", Warranty="3 năm"
        },
        new Product { 
            Name="Honda Vision 2024", Brand="Honda", Engine="110cc", Fuel="5.2L",
            Rating=4.7m, Price=32990000, ImageUrl="/images/Vision2024.jpg",
            CategoryId=tayGa.Id, Description="Sang trọng, tiết kiệm", Stock=80,
            Color="Bạc, Đen, Nâu", Warranty="3 năm"
        },
        new Product { 
            Name="Yamaha Janus Premium", Brand="Yamaha", Engine="125cc", Fuel="5.5L",
            Rating=4.6m, Price=33500000, Badge="hot", ImageUrl="/images/JanusPremium.jpg",
            CategoryId=tayGa.Id, Description="Thiết kế retro độc đáo", Stock=45,
            Color="Xanh Mint, Vàng, Hồng", Warranty="3 năm"
        },

        // ── Phụ tùng ──
        new Product {
            Name="Nhớt Castrol Power 1 10W-40", Brand="Castrol", Engine="1L", Fuel="N/A",
            Rating=4.8m, Price=185000, OldPrice=220000, DiscountPercent=16,
            ImageUrl="/images/Spare/nhot-castrol.jpg", CategoryId=phuTung.Id,
            Description="Nhớt xe số 4 thì cao cấp, bảo vệ động cơ tối ưu",
            Stock=200, Color="N/A", Warranty="12 tháng"
        },
        new Product {
            Name="Lốp Michelin City Grip 80/90-14", Brand="Michelin", Engine="80/90-14", Fuel="N/A",
            Rating=4.9m, Price=420000, Badge="hot",
            ImageUrl="/images/Spare/lop-michelin.jpg", CategoryId=phuTung.Id,
            Description="Lốp xe tay ga, độ bám đường tốt, chống trơn trượt",
            Stock=150, Color="Đen", Warranty="6 tháng"
        },
        new Product {
            Name="Ắc quy GS GTZ7V 12V-6Ah", Brand="GS Battery", Engine="12V-6Ah", Fuel="N/A",
            Rating=4.7m, Price=450000,
            ImageUrl="/images/Spare/acquy-gs.jpg", CategoryId=phuTung.Id,
            Description="Ắc quy khô MF, không cần bảo dưỡng",
            Stock=100, Color="N/A", Warranty="18 tháng"
        },
        new Product {
            Name="Phanh ABS Brembo Z04 cho Exciter", Brand="Brembo", Engine="N/A", Fuel="N/A",
            Rating=4.9m, Price=1250000, Badge="new",
            ImageUrl="/images/Spare/phanh-brembo.jpg", CategoryId=phuTung.Id,
            Description="Má phanh hiệu suất cao, độ bền vượt trội",
            Stock=80, Color="Đen", Warranty="24 tháng"
        },
        new Product {
            Name="Gương chiếu hậu KY Universal", Brand="KY", Engine="8mm/10mm", Fuel="N/A",
            Rating=4.5m, Price=145000, OldPrice=180000, DiscountPercent=19,
            ImageUrl="/images/Spare/guong-ky.jpg", CategoryId=phuTung.Id,
            Description="Gương tròn cao cấp, gắn được mọi loại xe",
            Stock=300, Color="Đen, Chrome", Warranty="6 tháng"
        },
        new Product {
            Name="Đèn LED trợ sáng L4X 40W", Brand="L4X", Engine="40W", Fuel="N/A",
            Rating=4.8m, Price=380000, Badge="hot",
            ImageUrl="/images/Spare/den-led-l4x.jpg", CategoryId=phuTung.Id,
            Description="Đèn LED siêu sáng, tiết kiệm điện",
            Stock=120, Color="Trắng, Vàng", Warranty="12 tháng"
        },
        new Product {
            Name="Baga GIVI E230 cho SH/Vision", Brand="GIVI", Engine="N/A", Fuel="N/A",
            Rating=4.9m, Price=2850000,
            ImageUrl="/images/Spare/baga-givi.jpg", CategoryId=phuTung.Id,
            Description="Thùng sau cao cấp, chứa được 1 nón bảo hiểm",
            Stock=50, Color="Đen, Bạc", Warranty="24 tháng"
        },
        new Product {
            Name="Yên độ Takano cho Winner/Exciter", Brand="Takano", Engine="N/A", Fuel="N/A",
            Rating=4.7m, Price=950000,
            ImageUrl="/images/Spare/yen-takano.jpg", CategoryId=phuTung.Id,
            Description="Yên cao su non, êm ái cho chặng đường dài",
            Stock=70, Color="Đen, Đỏ phối đen", Warranty="12 tháng"
        },
        new Product {
            Name="Khóa đĩa Kinbar chống trộm", Brand="Kinbar", Engine="N/A", Fuel="N/A",
            Rating=4.6m, Price=420000, OldPrice=500000, DiscountPercent=16,
            ImageUrl="/images/Spare/khoa-kinbar.jpg", CategoryId=phuTung.Id,
            Description="Khóa đĩa cao cấp, chống cắt, chống phá",
            Stock=150, Color="Vàng, Đỏ", Warranty="36 tháng"
        },
        new Product {
            Name="Bộ lọc gió DNA cho Air Blade/Vision", Brand="DNA", Engine="N/A", Fuel="N/A",
            Rating=4.8m, Price=680000, Badge="new",
            ImageUrl="/images/Spare/loc-gio-dna.jpg", CategoryId=phuTung.Id,
            Description="Lọc gió cao cấp, tăng công suất động cơ",
            Stock=90, Color="Đỏ, Xanh", Warranty="Trọn đời (có thể giặt tái sử dụng)"
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
            // Seed Seasonal Promotions
if (!db.SeasonalPromotions.Any())
{
    var now = DateTime.UtcNow;
    db.SeasonalPromotions.AddRange(
        new SeasonalPromotion
        {
            Name = "Flash Sale Cuối Tuần",
            Description = "Giảm 10% tất cả sản phẩm vào Thứ 7 & Chủ Nhật",
            DiscountPercent = 10,
            StartDate = now,
            EndDate = now.AddMonths(6),
            IsActive = true,
            ApplyTo = "All"
        },
        new SeasonalPromotion
        {
            Name = "Sale Xe Tay Ga",
            Description = "Giảm 15% cho tất cả xe tay ga",
            DiscountPercent = 15,
            StartDate = now,
            EndDate = now.AddMonths(3),
            IsActive = true,
            ApplyTo = "Category",
            CategoryId = 1
        },
        new SeasonalPromotion
        {
            Name = "Honda Sale",
            Description = "Giảm 12% cho xe Honda",
            DiscountPercent = 12,
            StartDate = now,
            EndDate = now.AddMonths(2),
            IsActive = true,
            ApplyTo = "Brand",
            Brand = "Honda"
        }
    );
    db.SaveChanges();
        }
    }
    }}