using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using MotoBikeStore.Services;
using System.Linq;
using System.Collections.Generic;

namespace MotoBikeStore.Controllers
{
    public class CompareController : Controller
    {
        const string COMPARE_KEY = "COMPARE_ITEMS";
        const int MAX_COMPARE = 4;
        
        // ✅ KHÔNG CẦN inject DbContext
        
        // Trang so sánh
        public IActionResult Index()
        {
            var items = HttpContext.Session.GetObjectFromJson<List<int>>(COMPARE_KEY) ?? new List<int>();
            
            // ✅ Dùng InMemoryDataStore
            var products = InMemoryDataStore.Products
                .Where(p => items.Contains(p.Id))
                .ToList();
            
            // Populate Category
            foreach (var product in products)
            {
                if (product.CategoryId.HasValue)
                {
                    product.Category = InMemoryDataStore.Categories
                        .FirstOrDefault(c => c.Id == product.CategoryId.Value);
                }
            }
            
            return View(products);
        }
        
        // Thêm sản phẩm vào danh sách so sánh
        public IActionResult Add(int id)
        {
            var items = HttpContext.Session.GetObjectFromJson<List<int>>(COMPARE_KEY) ?? new List<int>();
            
            if (items.Count >= MAX_COMPARE)
            {
                TempData["ErrorMessage"] = $"Chỉ có thể so sánh tối đa {MAX_COMPARE} sản phẩm cùng lúc!";
                return RedirectToAction("Index");
            }
            
            if (!items.Contains(id)) 
            {
                items.Add(id);
                TempData["SuccessMessage"] = "Đã thêm vào danh sách so sánh!";
            }
            else
            {
                TempData["InfoMessage"] = "Sản phẩm đã có trong danh sách so sánh!";
            }
            
            HttpContext.Session.SetObjectAsJson(COMPARE_KEY, items);
            return RedirectToAction("Index");
        }
        
        // Xóa khỏi danh sách so sánh
        public IActionResult Remove(int id)
        {
            var items = HttpContext.Session.GetObjectFromJson<List<int>>(COMPARE_KEY) ?? new List<int>();
            items.Remove(id);
            HttpContext.Session.SetObjectAsJson(COMPARE_KEY, items);
            
            TempData["SuccessMessage"] = "Đã xóa khỏi danh sách so sánh!";
            return RedirectToAction("Index");
        }
        
        // Xóa tất cả
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(COMPARE_KEY);
            TempData["SuccessMessage"] = "Đã xóa toàn bộ danh sách so sánh!";
            return RedirectToAction("Index");
        }
        
        // API: Lấy số lượng sản phẩm trong danh sách so sánh
        [HttpGet]
        public JsonResult Count()
        {
            var items = HttpContext.Session.GetObjectFromJson<List<int>>(COMPARE_KEY) ?? new List<int>();
            return Json(new { count = items.Count });
        }
    }
}