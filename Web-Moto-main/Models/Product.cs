using System.ComponentModel.DataAnnotations;

namespace MotoBikeStore.Models
{
    // Product - Cập nhật
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Engine { get; set; } = "";
        public string Fuel { get; set; } = "";
        public decimal Rating { get; set; }
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public string ImageUrl { get; set; } = "";
        public int? DiscountPercent { get; set; }
        public string? Badge { get; set; }
        
        // MỚI: Thêm Category
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        
        // MỚI: Thông số kỹ thuật chi tiết (cho so sánh)
        public string? Description { get; set; }
        public int? Stock { get; set; } = 100; // Tồn kho
        public string? Color { get; set; }
        public string? Warranty { get; set; } // Bảo hành
    }
    
    // Order - Cập nhật
    public class Order
    {
         public int Id { get; set; }
        
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string CustomerName { get; set; } = "";
        
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại phải có 10-11 chữ số")]
        public string Phone { get; set; } = "";
        
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string Address { get; set; } = "";
        
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        
        public string Status { get; set; } = "Pending";
        
        [Required(ErrorMessage = "Vui lòng chọn hình thức thanh toán")]
        public string PaymentMethod { get; set; } = "";
        
        public string OrderCode { get; set; } = "";
        
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        
        public int? CouponId { get; set; }
        public Coupon? Coupon { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; } = 0;
        public decimal Total { get; set; }
        
        public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
    }
    
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public Product? Product { get; set; }
        public Order? Order { get; set; }
    }
}