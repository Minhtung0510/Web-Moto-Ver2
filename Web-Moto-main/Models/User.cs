using System.ComponentModel.DataAnnotations;

namespace MotoBikeStore.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        
        [Required]
        public string PasswordHash { get; set; } = "";
        
        [Required]
        public string FullName { get; set; } = "";
        
        [Phone]
        public string? Phone { get; set; }
        
        public string? Address { get; set; }
        
        public string Role { get; set; } = "Customer"; // Customer, Admin
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}