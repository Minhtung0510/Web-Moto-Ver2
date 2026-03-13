using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using MotoBikeStore.Services;
using System.Linq;

namespace MotoBikeStore.Controllers
{
    public class ValidateCouponRequest
{
    public string? Code { get; set; }
    public decimal orderAmount { get; set; }
}
    public class CouponsController : Controller
    {
        private readonly MotoBikeContext _db;
        public CouponsController(MotoBikeContext context)
        {
            _db = context;
        }
        const string USER_KEY = "CURRENT_USER";
        
        private bool IsAdmin()
        {
            var sess = HttpContext.Session.GetObjectFromJson<UserSession>(USER_KEY);
            return sess != null && sess.Role == "Admin";
        }
        
        // ✅ API: Lấy danh sách coupon có sẵn cho checkout
        [HttpGet]
        public JsonResult GetAvailableCoupons(decimal orderAmount)
        {
            var now = DateTime.UtcNow;
            
            var availableCoupons = _db.Coupons
                .Where(c => 
                    c.IsActive && 
                    c.StartDate <= now && 
                    c.EndDate >= now &&
                    (c.UsageLimit == 0 || c.UsedCount < c.UsageLimit)
                )
                .OrderByDescending(c => 
            c.DiscountPercent > 0
        ? (
            c.MaxDiscountAmount.HasValue &&
            (orderAmount * c.DiscountPercent / 100) > c.MaxDiscountAmount.Value
                ? c.MaxDiscountAmount.Value
                : (orderAmount * c.DiscountPercent / 100)
          )
        : (c.DiscountAmount ?? 0)
                )
                .Select(c => new {
                    c.Id,
                    c.Code,
                    c.Description,
                    c.DiscountPercent,
                    c.DiscountAmount,
                    c.MinOrderAmount,
                    c.MaxDiscountAmount,
                    c.StartDate,
                    c.EndDate,
                    c.UsageLimit,
                    c.UsedCount
                })
                .ToList();
            
            return Json(availableCoupons);
        }
        
        // Danh sách mã giảm giá (Admin)
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var coupons = _db.Coupons
                .OrderByDescending(c => c.StartDate)
                .ToList();
            
            return View(coupons);
        }
        
        // GET: Tạo mã giảm giá
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            return View();
        }

        // POST: Tạo mã giảm giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePost()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var code = Request.Form["Code"].ToString().Trim().ToUpper();
            var discountPercent = decimal.TryParse(Request.Form["DiscountPercent"], out var dp) ? dp : 0;
            var endDateStr = Request.Form["EndDate"].ToString();
            
            Console.WriteLine($"[DEBUG COUPON] Code={code}, Percent={discountPercent}, EndDate={endDateStr}");
            
            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã giảm giá";
                return View("Create");
            }
            
            if (_db.Coupons.Any(c => 
                c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["ErrorMessage"] = "Mã giảm giá đã tồn tại";
                return View("Create");
            }
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z0-9]+$"))
            {
                TempData["ErrorMessage"] = "Mã chỉ được chứa chữ in hoa và số, không dấu";
                return View("Create");
            }
            
            if (discountPercent <= 0 || discountPercent > 100)
            {
                TempData["ErrorMessage"] = "Giảm giá phải từ 1% đến 100%";
                return View("Create");
            }
            
            if (!DateTime.TryParse(endDateStr, out var endDate))
            {
                TempData["ErrorMessage"] = "Ngày hết hạn không hợp lệ";
                return View("Create");
            }
            
            if (endDate.Date <= DateTime.Now.Date)
            {
                TempData["ErrorMessage"] = "Ngày hết hạn phải sau ngày hôm nay";
                return View("Create");
            }
            
            var coupon = new Coupon
            {
                Id = _db.Coupons.Max(c => c.Id) + 1,
                Code = code,
                Description = $"Giảm {discountPercent}% - Mã {code}",
                DiscountPercent = discountPercent,
                DiscountAmount = null,
                MinOrderAmount = 0,
                MaxDiscountAmount = null,
                StartDate = DateTime.UtcNow,
                EndDate = endDate,
                UsageLimit = 0,
                UsedCount = 0,
                IsActive = true
            };
            
            _db.Coupons.Add(coupon);
            _db.SaveChanges();
            
            Console.WriteLine($"[DEBUG COUPON] Added: ID={coupon.Id}, Code={coupon.Code}");
            Console.WriteLine($"[DEBUG COUPON] Total coupons: {_db.Coupons.Count()}");
            
            TempData["SuccessMessage"] = $"Tạo mã giảm giá {coupon.Code} thành công!";
            return RedirectToAction("Index");
        }
        
        // GET: Sửa mã giảm giá
        public IActionResult Edit(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var coupon = _db.Coupons.FirstOrDefault(c => c.Id == id);
            if (coupon == null) return NotFound();
            
            return View(coupon);
        }
        
        // POST: Sửa mã giảm giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPost(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var existing = _db.Coupons.FirstOrDefault(c => c.Id == id);
            if (existing == null) return NotFound();
            
            var discountPercent = decimal.TryParse(Request.Form["DiscountPercent"], out var dp) ? dp : existing.DiscountPercent;
            var endDateStr = Request.Form["EndDate"].ToString();
            var isActive = Request.Form["IsActive"].ToString() == "true";
            
            if (!DateTime.TryParse(endDateStr, out var endDate))
            {
                TempData["ErrorMessage"] = "Ngày hết hạn không hợp lệ";
                return View(existing);
            }
            
            existing.DiscountPercent = discountPercent;
            existing.EndDate = endDate;
            existing.IsActive = isActive;
            existing.Description = $"Giảm {discountPercent}% - Mã {existing.Code}";
            
            TempData["SuccessMessage"] = "Cập nhật mã giảm giá thành công!";
            return RedirectToAction("Index");
        }
        
        // GET: Xóa mã giảm giá
        public IActionResult Delete(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var coupon = _db.Coupons.FirstOrDefault(c => c.Id == id);
            if (coupon == null) return NotFound();
            
            return View(coupon);
        }
        
        // POST: Xóa mã giảm giá
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var coupon = _db.Coupons.FirstOrDefault(c => c.Id == id);
            if (coupon != null)
            {
                _db.Coupons.Remove(coupon);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Xóa mã giảm giá thành công!";
            }
            
            return RedirectToAction("Index");
        }
        
        // Toggle Active/Inactive
        public IActionResult ToggleStatus(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var coupon = _db.Coupons.FirstOrDefault(c => c.Id == id);
            if (coupon != null)
            {
                coupon.IsActive = !coupon.IsActive;
                TempData["SuccessMessage"] = coupon.IsActive 
                    ? "Đã kích hoạt mã giảm giá!" 
                    : "Đã vô hiệu hóa mã giảm giá!";
            }
            
            return RedirectToAction("Index");
        }

        // API: Validate coupon
       [HttpPost]
public JsonResult Validate([FromBody] ValidateCouponRequest request)
{
    var code = request?.Code?.Trim() ?? "";
    var orderAmount = request?.orderAmount ?? 0;
    
    Console.WriteLine($"[COUPON API] Validating: Code='{code}', Amount={orderAmount}");
    
    // Validate input
    if (string.IsNullOrWhiteSpace(code))
    {
        Console.WriteLine("[COUPON API] ❌ Empty code");
        return Json(new { valid = false, message = "Vui lòng nhập mã giảm giá" });
    }
    
    if (orderAmount <= 0)
    {
        Console.WriteLine("[COUPON API] ❌ Invalid amount");
        return Json(new { valid = false, message = "Giá trị đơn hàng không hợp lệ" });
    }
    
    var coupon = _db.Coupons.FirstOrDefault(c =>
        c.Code.Equals(code, StringComparison.OrdinalIgnoreCase) &&
        c.IsActive &&
        c.StartDate <= DateTime.UtcNow &&
        c.EndDate >= DateTime.UtcNow &&
        (c.UsageLimit == 0 || c.UsedCount < c.UsageLimit)
    );

    if (coupon == null)
    {
        Console.WriteLine($"[COUPON API] ❌ Coupon '{code}' not found");
        return Json(new { valid = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn" });
    }
    
    Console.WriteLine($"[COUPON API] ✅ Found: {coupon.Code}, MinAmount={coupon.MinOrderAmount:N0}");

    if (orderAmount < coupon.MinOrderAmount)
    {
        var msg = $"Đơn hàng tối thiểu {coupon.MinOrderAmount:N0}₫ để áp dụng mã này";
        Console.WriteLine($"[COUPON API] ❌ {msg}");
        return Json(new { valid = false, message = msg });
    }

    decimal discount = 0;
    if (coupon.DiscountPercent > 0)
    {
        discount = orderAmount * coupon.DiscountPercent / 100;
        if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
            discount = coupon.MaxDiscountAmount.Value;
    }
    else if (coupon.DiscountAmount.HasValue)
    {
        discount = coupon.DiscountAmount.Value;
    }
    
    Console.WriteLine($"[COUPON API] ✅ Valid! Discount={discount:N0}₫");

    return Json(new
    {
        valid = true,
        discount = discount,
        message = $"Giảm {discount:N0}₫",
        description = coupon.Description
    });
}
    }
}