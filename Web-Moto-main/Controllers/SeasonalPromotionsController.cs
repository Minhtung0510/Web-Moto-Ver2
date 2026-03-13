using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using MotoBikeStore.Services;

namespace MotoBikeStore.Controllers
{
    public class SeasonalPromotionsController : Controller
    {
        private readonly MotoBikeContext _db;
        public SeasonalPromotionsController(MotoBikeContext context)
        {
            _db = context;
        }
        const string USER_KEY = "CURRENT_USER";
        
        private bool IsAdmin()
        {
            var sess = HttpContext.Session.GetObjectFromJson<UserSession>(USER_KEY);
            return sess != null && sess.Role == "Admin";
        }
        
        // Danh sách khuyến mãi
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var promotions = SeasonalPromotionService.GetAll()
                .OrderByDescending(p => p.StartDate)
                .ToList();
            
            // Load categories for promotions that apply to specific categories
            var categoryIds = promotions
                .Where(p => p.ApplyTo == "Category" && p.CategoryId.HasValue)
                .Select(p => p.CategoryId.Value)
                .Distinct()
                .ToList();
            
            if (categoryIds.Any())
            {
                var categories = _db.Categories
                    .Where(c => categoryIds.Contains(c.Id))
                    .ToDictionary(c => c.Id);
                
                foreach (var promo in promotions.Where(p => p.ApplyTo == "Category" && p.CategoryId.HasValue))
                {
                    promo.Category = categories.GetValueOrDefault(promo.CategoryId.Value);
                }
            }
            
            return View(promotions);
        }
        
        // Tạo khuyến mãi
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            ViewBag.Categories = _db.Categories.ToList();
            ViewBag.Brands = _db.Products.Select(p => p.Brand).Distinct().ToList();
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SeasonalPromotion promo)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            if (string.IsNullOrWhiteSpace(promo.Name))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập tên chương trình";
                return View(promo);
            }
            
            if (promo.DiscountPercent <= 0 || promo.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Giảm giá phải từ 1% đến 100%";
                return View(promo);
            }
            
            if (promo.EndDate <= promo.StartDate)
            {
                TempData["ErrorMessage"] = "Ngày kết thúc phải sau ngày bắt đầu";
                return View(promo);
            }
            
            SeasonalPromotionService.Add(promo);
            TempData["SuccessMessage"] = $"Tạo chương trình '{promo.Name}' thành công!";
            return RedirectToAction("Index");
        }
        
        // Toggle Active/Inactive
        public IActionResult Toggle(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            SeasonalPromotionService.Toggle(id);
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái!";
            return RedirectToAction("Index");
        }
        
        // Xóa
        public IActionResult Delete(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            SeasonalPromotionService.Remove(id);
            TempData["SuccessMessage"] = "Đã xóa chương trình khuyến mãi!";
            return RedirectToAction("Index");
        }
    }
}