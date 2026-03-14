using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using MotoBikeStore.Services;
using MotoBikeStore.Controllers;
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

    public IActionResult Index()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Auth");

        var promotions = _db.SeasonalPromotions
            .OrderByDescending(p => p.StartDate)
            .ToList();

        // Load category names
        foreach (var promo in promotions.Where(p =>
            p.ApplyTo == "Category" && p.CategoryId.HasValue))
        {
            promo.Category = _db.Categories
                .FirstOrDefault(c => c.Id == promo.CategoryId);
        }

        return View(promotions);
    }

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

        // ✅ Lưu vào DB thay vì in-memory
        _db.SeasonalPromotions.Add(promo);
        _db.SaveChanges();

        TempData["SuccessMessage"] = $"Tạo chương trình '{promo.Name}' thành công!";
        return RedirectToAction("Index");
    }

    public IActionResult Toggle(int id)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Auth");

        var promo = _db.SeasonalPromotions.FirstOrDefault(p => p.Id == id);
        if (promo != null)
        {
            promo.IsActive = !promo.IsActive;
            _db.SaveChanges();
        }

        TempData["SuccessMessage"] = "Đã cập nhật trạng thái!";
        return RedirectToAction("Index");
    }

    public IActionResult Delete(int id)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Auth");

        var promo = _db.SeasonalPromotions.FirstOrDefault(p => p.Id == id);
        if (promo != null)
        {
            _db.SeasonalPromotions.Remove(promo);
            _db.SaveChanges();
        }

        TempData["SuccessMessage"] = "Đã xóa chương trình khuyến mãi!";
        return RedirectToAction("Index");
    }
}