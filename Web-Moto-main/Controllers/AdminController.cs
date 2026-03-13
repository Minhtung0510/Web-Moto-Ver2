using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using MotoBikeStore.Services;
using System.Linq;

namespace MotoBikeStore.Controllers
{
    public class AdminController : Controller
    {
        const string USER_KEY = "CURRENT_USER";

        private bool IsAdmin()
        {
            var sess = HttpContext.Session.GetObjectFromJson<UserSession>(USER_KEY);
            return sess != null && sess.Role == "Admin";
        }

        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var today = DateTime.UtcNow.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var orders = InMemoryDataStore.Orders;
            var users = InMemoryDataStore.Users;
            var products = InMemoryDataStore.Products;

            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalRevenue = orders.Sum(o => (decimal?)o.Total) ?? 0;
            ViewBag.TotalProducts = products.Count;
            ViewBag.TotalCustomers = users.Count(u => u.Role == "Customer");
            ViewBag.MonthlyRevenue = orders.Where(o => o.OrderDate >= thisMonth)
                                            .Sum(o => (decimal?)o.Total) ?? 0;
            ViewBag.PendingOrders = orders.Count(o => o.Status == "Pending");

            var topProducts = orders
                .SelectMany(o => o.Details)
                .GroupBy(d => d.ProductId)
                .Select(g => new
                {
                    Product = products.FirstOrDefault(p => p.Id == g.Key),
                    TotalSold = g.Sum(d => d.Quantity)
                })
                .Where(x => x.Product != null)
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToList();
            ViewBag.TopProducts = topProducts;

            var recentOrders = orders
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToList();
            ViewBag.RecentOrders = recentOrders;

            return View();
        }

        public IActionResult Orders(string? status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var q = InMemoryDataStore.Orders.AsEnumerable();
            if (!string.IsNullOrEmpty(status))
                q = q.Where(o => o.Status == status);

            var list = q.OrderByDescending(o => o.OrderDate).ToList();
            ViewBag.SelectedStatus = status;
            return View(list);
        }

        public IActionResult OrderDetail(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var order = InMemoryDataStore.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            // nạp Product cho mỗi OrderDetail để View dùng
            foreach (var d in order.Details)
                d.Product = InMemoryDataStore.Products.FirstOrDefault(p => p.Id == d.ProductId);

            // nạp Coupon nếu cần (nếu bạn lưu coupon in-memory, thêm store tương ứng)
            return View(order);
        }

        [HttpPost]
        public IActionResult UpdateOrderStatus(int id, string status, string? trackingNumber)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var order = InMemoryDataStore.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            order.Status = status;
            if (status == "Shipping" && !string.IsNullOrEmpty(trackingNumber))
            {
                order.TrackingNumber = trackingNumber;
                order.ShippedDate = DateTime.UtcNow;
            }
            else if (status == "Delivered")
            {
                order.DeliveredDate = DateTime.UtcNow;
            }

            TempData["SuccessMessage"] = "Đã cập nhật trạng thái đơn hàng!";
            return RedirectToAction("OrderDetail", new { id });
        }

       public IActionResult Reports(int? year, int? month)
{
    if (!IsAdmin()) return RedirectToAction("Login", "Auth");

    year ??= DateTime.UtcNow.Year;
    month ??= DateTime.UtcNow.Month;

    var orders = InMemoryDataStore.Orders.Where(o => o.Status != "Cancelled").ToList();

    // ✅ QUAN TRỌNG: Phải dùng lowercase!
    var monthlyRevenue = orders
        .Where(o => o.OrderDate.Year == year)
        .GroupBy(o => o.OrderDate.Month)
        .Select(g => new
        {
            month = g.Key,                    // ← lowercase m
            revenue = g.Sum(o => o.Total),    // ← lowercase r
            orderCount = g.Count()            // ← lowercase o
        })
        .OrderBy(x => x.month)
        .ToList();
    
    ViewBag.MonthlyRevenue = monthlyRevenue;
    ViewBag.Year = year;
    ViewBag.Month = month;

    var start = new DateTime(year.Value, month.Value, 1);
    var end = start.AddMonths(1);

    var dailyRevenue = orders
        .Where(o => o.OrderDate >= start && o.OrderDate < end)
        .GroupBy(o => o.OrderDate.Date)
        .Select(g => new
        {
            date = g.Key,                     // ← lowercase d
            revenue = g.Sum(o => o.Total),    // ← lowercase r
            orderCount = g.Count()            // ← lowercase o
        })
        .OrderBy(x => x.date)
        .ToList();
    
    ViewBag.DailyRevenue = dailyRevenue;

    var topCustomers = orders
        .Where(o => o.UserId != null)
        .GroupBy(o => o.UserId!.Value)
        .Select(g => new
        {
            UserId = g.Key,
            TotalSpent = g.Sum(o => o.Total),
            OrderCount = g.Count()
        })
        .OrderByDescending(x => x.TotalSpent)
        .Take(10)
        .ToList();

    var users = InMemoryDataStore.Users;
    ViewBag.TopCustomers = topCustomers.Select(tc => new
    {
        Customer = users.FirstOrDefault(u => u.Id == tc.UserId),
        tc.TotalSpent,
        tc.OrderCount
    }).Where(x => x.Customer != null).ToList();

    return View();
}


        // Danh sách users
public IActionResult Users()
{
    if (!IsAdmin()) return RedirectToAction("Login", "Auth");
    
    var users = InMemoryDataStore.Users
        .OrderByDescending(u => u.CreatedAt)
        .ToList();
    
    return View(users);
}
// Chi tiết user
public IActionResult UserDetail(int id)
{
    if (!IsAdmin()) return RedirectToAction("Login", "Auth");
    
    var user = InMemoryDataStore.Users.FirstOrDefault(u => u.Id == id);
    if (user == null) return NotFound();
    
    // Lấy đơn hàng của user
    var orders = InMemoryDataStore.Orders
        .Where(o => o.UserId == id)
        .OrderByDescending(o => o.OrderDate)
        .ToList();
    
    ViewBag.Orders = orders;
    ViewBag.TotalOrders = orders.Count;
    ViewBag.TotalSpent = orders.Sum(o => o.Total);
    
    return View(user);
}

// Toggle Active/Inactive
public IActionResult ToggleUserStatus(int id)
{
    if (!IsAdmin()) return RedirectToAction("Login", "Auth");
    
    var user = InMemoryDataStore.Users.FirstOrDefault(u => u.Id == id);
    if (user != null && user.Role != "Admin") // Không khóa Admin
    {
        user.IsActive = !user.IsActive;
        TempData["SuccessMessage"] = user.IsActive 
            ? $"Đã mở khóa tài khoản {user.Email}" 
            : $"Đã khóa tài khoản {user.Email}";
    }
    
    return RedirectToAction("Users");
}
    }
    

}
