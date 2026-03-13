// Services/SeasonalPromotionService.cs
using MotoBikeStore.Models;

namespace MotoBikeStore.Services
{
    public class SeasonalPromotion
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Áp dụng cho
        public string? ApplyTo { get; set; } // "All", "Category", "Brand"
        public int? CategoryId { get; set; }
        public string? Brand { get; set; }
    }
    
    public static class SeasonalPromotionService
    {
        private static List<SeasonalPromotion> Promotions = new();
        private static int _counter = 1;
        
        public static void Initialize()
        {
            if (Promotions.Any()) return;
            
            var now = DateTime.UtcNow;
            
            Promotions = new List<SeasonalPromotion>
            {
                // Flash Sale cuối tuần
                new SeasonalPromotion
                {
                    Id = _counter++,
                    Name = "Flash Sale Cuối Tuần",
                    Description = "Giảm 10% tất cả sản phẩm vào Thứ 7 & Chủ Nhật",
                    DiscountPercent = 10,
                    StartDate = now,
                    EndDate = now.AddMonths(6),
                    IsActive = true,
                    ApplyTo = "All"
                },
                
                // Giảm giá Xe Tay Ga
                new SeasonalPromotion
                {
                    Id = _counter++,
                    Name = "Sale Xe Tay Ga",
                    Description = "Giảm 15% cho tất cả xe tay ga",
                    DiscountPercent = 15,
                    StartDate = now,
                    EndDate = now.AddMonths(3),
                    IsActive = true,
                    ApplyTo = "Category",
                    CategoryId = 1
                },
                
                // Giảm giá thương hiệu Honda
                new SeasonalPromotion
                {
                    Id = _counter++,
                    Name = "Honda Sale",
                    Description = "Giảm 12% cho xe Honda",
                    DiscountPercent = 12,
                    StartDate = now,
                    EndDate = now.AddMonths(2),
                    IsActive = true,
                    ApplyTo = "Brand",
                    Brand = "Honda"
                }
            };
        }
        
        // Lấy khuyến mãi áp dụng cho sản phẩm
        public static decimal GetBestDiscount(Product product)
        {
            var now = DateTime.UtcNow;
            var isWeekend = now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
            
            var applicablePromotions = Promotions.Where(p =>
                p.IsActive &&
                p.StartDate <= now &&
                p.EndDate >= now &&
                (
                    p.ApplyTo == "All" ||
                    (p.ApplyTo == "Category" && p.CategoryId == product.CategoryId) ||
                    (p.ApplyTo == "Brand" && p.Brand == product.Brand)
                )
            ).ToList();
            
            // Flash sale cuối tuần - chỉ áp dụng Thứ 7 & CN
            applicablePromotions = applicablePromotions.Where(p => 
                p.ApplyTo != "All" || 
                (p.ApplyTo == "All" && p.Name.Contains("Cuối Tuần") && isWeekend) ||
                (p.ApplyTo == "All" && !p.Name.Contains("Cuối Tuần"))
            ).ToList();
            
            if (!applicablePromotions.Any()) return 0;
            
            return applicablePromotions.Max(p => p.DiscountPercent);
        }
        
        public static List<SeasonalPromotion> GetActivePromotions()
        {
            var now = DateTime.UtcNow;
            return Promotions.Where(p => 
                p.IsActive && 
                p.StartDate <= now && 
                p.EndDate >= now
            ).ToList();
        }
        
        public static List<SeasonalPromotion> GetAll() => Promotions;
        
        public static void Add(SeasonalPromotion promo)
        {
            promo.Id = _counter++;
            Promotions.Add(promo);
        }
        
        public static void Remove(int id)
        {
            var promo = Promotions.FirstOrDefault(p => p.Id == id);
            if (promo != null) Promotions.Remove(promo);
        }
        
        public static void Toggle(int id)
        {
            var promo = Promotions.FirstOrDefault(p => p.Id == id);
            if (promo != null) promo.IsActive = !promo.IsActive;
        }
    }
}