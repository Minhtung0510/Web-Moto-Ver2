using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotoBikeStore.Models;
using MotoBikeStore.Services;
using System.Linq;

namespace MotoBikeStore.Controllers
{
    public class OrdersController : Controller
    {
        private readonly MotoBikeContext _db;
        public OrdersController(MotoBikeContext context)
        {
            _db = context;
        }
        const string CART_KEY = "CART_ITEMS";
        const string USER_KEY = "CURRENT_USER";

        // GET: /Orders/Checkout
        public IActionResult Checkout()
        {
            var ids = HttpContext.Session.GetObjectFromJson<List<int>>(CART_KEY) ?? new List<int>();
            if (!ids.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống!";
                return RedirectToAction("Index", "Cart");
            }

            var products = _db.Products.Where(p => ids.Contains(p.Id)).ToList();
            ViewBag.Products = products;
            ViewBag.Subtotal = products.Sum(p => p.Price);

            // Seasonal cho UI
            var activePromos = SeasonalPromotionService.GetActivePromotions();
            ViewBag.SeasonalPromotions = activePromos;

            decimal potentialSeasonal = 0m;
            foreach (var product in products)
            {
                var bestPercent = SeasonalPromotionService.GetBestDiscount(product);
                if (bestPercent > 0) potentialSeasonal += product.Price * bestPercent / 100m;
            }
            ViewBag.PotentialSeasonalDiscount = potentialSeasonal;

            return View(new Order());
        }

        // POST: /Orders/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(Order order, string? couponCode)
        {
            var ids = HttpContext.Session.GetObjectFromJson<List<int>>(CART_KEY) ?? new List<int>();
            if (!ids.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống!";
                return RedirectToAction("Index", "Cart");
            }

            var products = _db.Products.Where(p => ids.Contains(p.Id)).ToList();
            ViewBag.Products = products;
            ViewBag.Subtotal = products.Sum(p => p.Price);

            // Bind lại seasonal UI nếu trả về View(order)
            var activePromos = SeasonalPromotionService.GetActivePromotions();
            ViewBag.SeasonalPromotions = activePromos;
            decimal potentialSeasonal = 0m;
            foreach (var product in products)
            {
                var bestPercent = SeasonalPromotionService.GetBestDiscount(product);
                if (bestPercent > 0) potentialSeasonal += product.Price * bestPercent / 100m;
            }
            ViewBag.PotentialSeasonalDiscount = potentialSeasonal;

            // Validate tối thiểu
            bool hasError = false;
            if (string.IsNullOrWhiteSpace(order.CustomerName)) { ModelState.AddModelError("CustomerName", "Vui lòng nhập họ tên"); hasError = true; }
            if (string.IsNullOrWhiteSpace(order.Phone)) { ModelState.AddModelError("Phone", "Vui lòng nhập số điện thoại"); hasError = true; }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(order.Phone, @"^[0-9]{10,11}$")) { ModelState.AddModelError("Phone", "Số điện thoại phải có 10-11 chữ số"); hasError = true; }
            if (string.IsNullOrWhiteSpace(order.Address)) { ModelState.AddModelError("Address", "Vui lòng nhập địa chỉ giao hàng"); hasError = true; }
            if (string.IsNullOrWhiteSpace(order.PaymentMethod)) { ModelState.AddModelError("PaymentMethod", "Vui lòng chọn hình thức thanh toán"); hasError = true; }
            if (hasError) return View(order);

            // Build order
            order.Details = products.Select(p => new OrderDetail
            {
                OrderId = order.Id,
                ProductId = p.Id,
                Quantity = 1,
                UnitPrice = p.Price
            }).ToList();

            order.Subtotal = products.Sum(p => p.Price);
            order.ShippingFee = order.Subtotal >= 5_000_000 ? 0 : 150_000;
            order.DiscountAmount = 0m;

            // TÍNH SEASONAL
            decimal seasonalDiscountAmount = 0m;
            string seasonalPromotionApplied = "";
            foreach (var product in products)
            {
                var bestPercent = SeasonalPromotionService.GetBestDiscount(product);
                if (bestPercent > 0)
                {
                    seasonalDiscountAmount += product.Price * bestPercent / 100m;

                    var applied = activePromos.FirstOrDefault(p =>
                        p.ApplyTo == "All" ||
                        (p.ApplyTo == "Category" && p.CategoryId == product.CategoryId) ||
                        (p.ApplyTo == "Brand" && p.Brand == product.Brand)
                    );
                    if (applied != null && string.IsNullOrEmpty(seasonalPromotionApplied))
                        seasonalPromotionApplied = applied.Name;
                }
            }

            // TÍNH COUPON
            decimal couponDiscountAmount = 0m;
            Coupon? appliedCoupon = null;

            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                appliedCoupon = _db.Coupons.FirstOrDefault(c =>
                    c.Code.Equals(couponCode, StringComparison.OrdinalIgnoreCase) &&
                    c.IsActive &&
                    c.StartDate <= DateTime.UtcNow && c.EndDate >= DateTime.UtcNow &&
                    order.Subtotal >= c.MinOrderAmount &&
                    (c.UsageLimit == 0 || c.UsedCount < c.UsageLimit)
                );

                if (appliedCoupon != null)
                {
                    if (appliedCoupon.DiscountPercent > 0)
                    {
                        couponDiscountAmount = order.Subtotal * appliedCoupon.DiscountPercent / 100m;
                        if (appliedCoupon.MaxDiscountAmount.HasValue && couponDiscountAmount > appliedCoupon.MaxDiscountAmount.Value)
                            couponDiscountAmount = appliedCoupon.MaxDiscountAmount.Value;
                    }
                    else if (appliedCoupon.DiscountAmount.HasValue)
                    {
                        couponDiscountAmount = appliedCoupon.DiscountAmount.Value;
                    }
                }
            }

            // Nếu không nhập hoặc không hợp lệ → tự tìm mã tốt nhất
            if (appliedCoupon == null)
            {
                var available = _db.Coupons.Where(c =>
                    c.IsActive &&
                    c.StartDate <= DateTime.UtcNow && c.EndDate >= DateTime.UtcNow &&
                    order.Subtotal >= c.MinOrderAmount &&
                    (c.UsageLimit == 0 || c.UsedCount < c.UsageLimit)
                ).ToList();

                decimal bestAmt = 0m;
                Coupon? best = null;
                foreach (var c in available)
                {
                    decimal amt = 0m;
                    if (c.DiscountPercent > 0)
                    {
                        amt = order.Subtotal * c.DiscountPercent / 100m;
                        if (c.MaxDiscountAmount.HasValue && amt > c.MaxDiscountAmount.Value) amt = c.MaxDiscountAmount.Value;
                    }
                    else if (c.DiscountAmount.HasValue) amt = c.DiscountAmount.Value;

                    if (amt > bestAmt) { bestAmt = amt; best = c; }
                }
                if (best != null) { appliedCoupon = best; couponDiscountAmount = bestAmt; }
            }

            // ÁP DỤNG: CỘNG DỒN CẢ HAI
            order.DiscountAmount = seasonalDiscountAmount + couponDiscountAmount;

            if (appliedCoupon != null && couponDiscountAmount > 0)
            {
                order.CouponId = appliedCoupon.Id;
                appliedCoupon.UsedCount++;
            }

            order.Total = order.Subtotal + order.ShippingFee - order.DiscountAmount;
            order.OrderDate = DateTime.UtcNow;
            order.Status = "Pending";

            var sess = HttpContext.Session.GetObjectFromJson<UserSession>(USER_KEY);
            if (sess != null) order.UserId = sess.Id;

            _db .Orders.Add(order);
            _db.SaveChanges();
            order.OrderCode = $"MB-{DateTime.UtcNow:yyyyMMdd}-{order.Id:D4}";
            _db.SaveChanges();
            HttpContext.Session.Remove(CART_KEY);

            // Thông báo
            var parts = new List<string>();
            if (seasonalDiscountAmount > 0 && !string.IsNullOrEmpty(seasonalPromotionApplied))
                parts.Add($"Khuyến mãi '{seasonalPromotionApplied}' - Giảm {seasonalDiscountAmount:N0}₫");
            if (appliedCoupon != null && couponDiscountAmount > 0)
                parts.Add($"Mã {appliedCoupon.Code} - Giảm {couponDiscountAmount:N0}₫");
            if (parts.Any()) TempData["CouponApplied"] = "Áp dụng ưu đãi: " + string.Join(" + ", parts);

            Console.WriteLine($"[ORDER] ID={order.Id}, Subtotal={order.Subtotal:N0}, Seasonal={seasonalDiscountAmount:N0}, Coupon={couponDiscountAmount:N0}, Total={order.Total:N0}, Method={order.PaymentMethod}");

            // Redirect
            if (order.PaymentMethod == "Chuyển khoản")
                return RedirectToAction("BankTransfer", new { id = order.Id });

            foreach (var d in order.Details)
                d.Product = _db.Products.FirstOrDefault(p => p.Id == d.ProductId);
            if (order.CouponId.HasValue)
                order.Coupon = _db.Coupons.FirstOrDefault(c => c.Id == order.CouponId.Value);

            // Pass thêm để Success view hiển thị chi tiết
            ViewBag.SeasonalAppliedAmount = seasonalDiscountAmount;
            ViewBag.CouponAppliedAmount = couponDiscountAmount;
            ViewBag.CouponAppliedCode = appliedCoupon?.Code;

            return View("Success", order);
        }

        // GET: /Orders/BankTransfer/{id}
        public IActionResult BankTransfer(int id)
        {
            var order = _db.Orders
                .Include(o => o.Details)
                .FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.Details != null)
            {
                foreach (var d in order.Details)
                    d.Product = _db.Products.FirstOrDefault(p => p.Id == d.ProductId);
            }

            if (order.CouponId.HasValue)
                order.Coupon = _db.Coupons.FirstOrDefault(c => c.Id == order.CouponId.Value);

            return View(order);
        }

        // GET: /Orders/Track?id=MB-... hoặc id=123
        public IActionResult Track(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return View((Order?)null);

            try
            {
                Order? order = _db.Orders
                    .Include(o => o.Details)
                    .FirstOrDefault(o => !string.IsNullOrEmpty(o.OrderCode) &&
                        o.OrderCode.Equals(id, StringComparison.OrdinalIgnoreCase));

                if (order == null && int.TryParse(id, out var orderId))
                    order = _db.Orders
                        .Include(o => o.Details)
                        .FirstOrDefault(o => o.Id == orderId);

                if (order != null && order.Details != null)
                {
                    foreach (var d in order.Details)
                        d.Product = _db.Products.FirstOrDefault(p => p.Id == d.ProductId);
                }

                return View(order);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Track] Error: " + ex);
                // Trả về view trống thay vì ném lỗi 500
                return View((Order?)null);
            }
        }

        // GET: /Orders/MyOrders
        public IActionResult MyOrders()
        {
            var sess = HttpContext.Session.GetObjectFromJson<UserSession>(USER_KEY);
            if (sess == null) return RedirectToAction("Login", "Auth");

            var userId = (int)sess.Id;
            var orders = _db.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Details)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            foreach (var o in orders)
            {
                if (o.Details == null) continue;
                foreach (var d in o.Details)
                    d.Product = _db.Products.FirstOrDefault(p => p.Id == d.ProductId);
            }

            return View(orders);
        }
    }
}
