using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using MotoBikeStore.Services;

namespace MotoBikeStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly MotoBikeContext _db;
        public HomeController(MotoBikeContext context)
        {
            _db = context;
        }
        // ✅ KHÔNG CẦN inject DbContext

        public IActionResult Index(string? brand, string? q, int? categoryId,
            decimal? minPrice, decimal? maxPrice, string? sortBy, string? categoryName)
        {
            // ✅ Dùng InMemoryDataStore thay vì _db
            var query = _db.Products.AsQueryable();
            if (categoryId == null && !string.IsNullOrEmpty(categoryName))
            {
                var cat = _db.Categories
                    .FirstOrDefault(c => c.Name == categoryName);
                if (cat != null) categoryId = cat.Id;
            }

            // Filters
            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(p => p.Brand == brand);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(q) ||
                    (p.Description != null && p.Description.ToLower().Contains(q))
                );
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Sorting
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Name),
                "rating" => query.OrderByDescending(p => p.Rating),
                _ => query.OrderBy(p => p.Brand).ThenBy(p => p.Name)
            };

            var products = query.ToList();

            // Populate categories
            foreach (var product in products)
            {
                if (product.CategoryId.HasValue)
                {
                    product.Category = _db.Categories
                        .FirstOrDefault(c => c.Id == product.CategoryId.Value);
                }
            }

            // ViewBag data
            ViewBag.Categories = _db.Categories.ToList();
            ViewBag.Brands = _db.Products
                .Select(p => p.Brand)
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            ViewBag.SelectedBrand = brand;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchQuery = q;
            ViewBag.SortBy = sortBy;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(products);
        }

        public IActionResult Detail(int id)
        {
            var product = _db.Products
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return NotFound();

            // Populate category
            if (product.CategoryId.HasValue)
            {
                product.Category = _db.Categories
                    .FirstOrDefault(c => c.Id == product.CategoryId.Value);
            }

            // Related products
            var relatedProducts = _db.Products
                .Where(p => p.Id != id &&
                    (p.Brand == product.Brand ||
                     p.CategoryId == product.CategoryId))
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }
    }
}