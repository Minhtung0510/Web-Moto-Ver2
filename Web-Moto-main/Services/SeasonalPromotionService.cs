// Services/SeasonalPromotionService.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MotoBikeStore.Models;

namespace MotoBikeStore.Services
{
    public class SeasonalPromotion
    {

    
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = "";
        
        public string Description { get; set; } = "";
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPercent { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string? ApplyTo { get; set; } // "All", "Category", "Brand"
        
        public int? CategoryId { get; set; }
        
        public string? Brand { get; set; }
        
        [NotMapped] // Không lưu vào DB, chỉ dùng để hiển thị
        public Category? Category { get; set; }
    }
    }
    namespace MotoBikeStore.Services
{
    // Service chỉ còn hàm tính toán, không lưu trữ nữa
    public static class SeasonalPromotionService
    {
        public static decimal GetBestDiscount(
            MotoBikeStore.Models.Product product,
            List<SeasonalPromotion> activePromotions)
        {
            var now = DateTime.UtcNow;
            var isWeekend = now.DayOfWeek == DayOfWeek.Saturday 
                         || now.DayOfWeek == DayOfWeek.Sunday;

            var applicable = activePromotions.Where(p =>
                p.IsActive &&
                p.StartDate <= now &&
                p.EndDate >= now &&
                (
                    p.ApplyTo == "All" ||
                    (p.ApplyTo == "Category" && p.CategoryId == product.CategoryId) ||
                    (p.ApplyTo == "Brand" && p.Brand == product.Brand)
                )
            ).ToList();

            // Flash sale cuối tuần - chỉ áp dụng T7 & CN
            applicable = applicable.Where(p =>
                p.ApplyTo != "All" ||
                (p.ApplyTo == "All" && p.Name.Contains("Cuối Tuần") && isWeekend) ||
                (p.ApplyTo == "All" && !p.Name.Contains("Cuối Tuần"))
            ).ToList();

            if (!applicable.Any()) return 0;

            return applicable.Max(p => p.DiscountPercent);
        }

        public static List<SeasonalPromotion> GetActivePromotions(
            List<SeasonalPromotion> allPromotions)
        {
            var now = DateTime.UtcNow;
            return allPromotions.Where(p =>
                p.IsActive &&
                p.StartDate <= now &&
                p.EndDate >= now
            ).ToList();
        }
    }
}
   
