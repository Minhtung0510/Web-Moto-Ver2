using System.ComponentModel.DataAnnotations;

namespace MotoBikeStore.Models
{
    // Danh mục sản phẩm
    public class Category
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = ""; // Xe tay ga, Xe số, Phụ tùng
        
        public string? Description { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
    
    // Mã giảm giá
    public class Coupon
    {
        public int Id { get; set; }
        
        [Required]
        public string Code { get; set; } = ""; // VD: SUMMER2024
        
        public string Description { get; set; } = "";
        
        public decimal DiscountPercent { get; set; } // Giảm theo %
        
        public decimal? DiscountAmount { get; set; } // Hoặc giảm số tiền cố định
        
        public decimal MinOrderAmount { get; set; } = 0; // Đơn tối thiểu
        
        public decimal? MaxDiscountAmount { get; set; } // Giảm tối đa
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public int UsageLimit { get; set; } = 0; // 0 = không giới hạn
        
        public int UsedCount { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}