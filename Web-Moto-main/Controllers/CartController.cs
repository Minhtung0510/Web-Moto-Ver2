using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using System.Linq;
using System.Collections.Generic;
using MotoBikeStore.Services;

namespace MotoBikeStore.Controllers
{
    public class CartController : Controller
    {
            const string CART_KEY = "CART_ITEMS";

        public IActionResult Index()
        {
            var ids = HttpContext.Session.GetObjectFromJson<List<int>>(CART_KEY) ?? new List<int>();
            var products = InMemoryDataStore.Products.Where(p => ids.Contains(p.Id)).ToList(); // ✅
            return View(products);
        }

        public IActionResult Add(int id)
        {
            // ✅ đảm bảo sản phẩm tồn tại trong InMemory
            if (!InMemoryDataStore.Products.Any(p => p.Id == id))
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index","Home");
            }

            var ids = HttpContext.Session.GetObjectFromJson<List<int>>(CART_KEY) ?? new List<int>();
            if (!ids.Contains(id)) ids.Add(id);
            HttpContext.Session.SetObjectAsJson(CART_KEY, ids);
            TempData["SuccessMessage"] = "Đã thêm vào giỏ.";
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            var ids = HttpContext.Session.GetObjectFromJson<List<int>>(CART_KEY) ?? new List<int>();
            ids.Remove(id);
            HttpContext.Session.SetObjectAsJson(CART_KEY, ids);
            return RedirectToAction("Index");
        }
    }
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value) =>
            session.SetString(key, System.Text.Json.JsonSerializer.Serialize(value));
        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(value);
        }
    }
}
