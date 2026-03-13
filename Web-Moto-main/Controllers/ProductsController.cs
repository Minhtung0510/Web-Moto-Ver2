using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using MotoBikeStore.Services;
using System.Globalization;
namespace MotoBikeStore.Controllers
{
    public class ProductsController : Controller
    {
        const string USER_KEY = "CURRENT_USER";
        private readonly IWebHostEnvironment _env;
        
        // ✅ Inject IWebHostEnvironment để lấy đường dẫn wwwroot
        public ProductsController(IWebHostEnvironment env)
        {
            _env = env;
        }
        
        private bool IsAdmin()
        {
            var sess = HttpContext.Session.GetObjectFromJson<UserSession>(USER_KEY);
            return sess != null && sess.Role == "Admin";
        }
        
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var products = InMemoryDataStore.Products.ToList();
            return View(products);
        }
        
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            return View();
        }
        
      [HttpPost]
public async Task<IActionResult> Create(IFormFile ImageFile)  // ✅ CHỈ nhận ImageFile
{
    if (!IsAdmin()) return RedirectToAction("Login", "Auth");
    
    // ✅ MANUAL BINDING từ Request.Form
    var product = new Product
    {
        Name = Request.Form["Name"].ToString(),
        Brand = Request.Form["Brand"].ToString(),
        Engine = Request.Form["Engine"].ToString(),
        Fuel = Request.Form["Fuel"].ToString(),
        Price = decimal.TryParse(Request.Form["Price"], out var price) ? price : 0,
        OldPrice = decimal.TryParse(Request.Form["OldPrice"], out var oldPrice) && oldPrice > 0 ? oldPrice : null,
        DiscountPercent = int.TryParse(Request.Form["DiscountPercent"], out var discount) && discount > 0 ? discount : null,
        Rating = decimal.TryParse(Request.Form["Rating"], out var rating) ? rating : 4.5m,
        Stock = int.TryParse(Request.Form["Stock"], out var stock) ? stock : 100,
        CategoryId = int.TryParse(Request.Form["CategoryId"], out var catId) && catId > 0 ? catId : null,
        Badge = Request.Form["Badge"].ToString(),
        Color = Request.Form["Color"].ToString(),
        Warranty = Request.Form["Warranty"].ToString(),
        Description = Request.Form["Description"].ToString()
    };
    
    // ✅ DEBUG
    Console.WriteLine($"[DEBUG] Name={product.Name}, Price={product.Price}, Brand={product.Brand}");
    Console.WriteLine($"[DEBUG] ImageFile={ImageFile?.FileName ?? "NULL"}");
    
    // ✅ VALIDATE
    if (string.IsNullOrWhiteSpace(product.Name))
    {
        TempData["ErrorMessage"] = "Vui lòng nhập tên sản phẩm";
        return View(product);
    }
    
    if (product.Price <= 0)
    {
        TempData["ErrorMessage"] = "Vui lòng nhập giá sản phẩm hợp lệ";
        return View(product);
    }
    
    if (string.IsNullOrWhiteSpace(product.Brand))
    {
        TempData["ErrorMessage"] = "Vui lòng chọn thương hiệu";
        return View(product);
    }
    
    if (string.IsNullOrWhiteSpace(product.Engine))
    {
        TempData["ErrorMessage"] = "Vui lòng nhập động cơ";
        return View(product);
    }
    
    if (string.IsNullOrWhiteSpace(product.Fuel))
    {
        TempData["ErrorMessage"] = "Vui lòng nhập nhiên liệu";
        return View(product);
    }
    
    // ✅ VALIDATE IMAGE
    if (ImageFile == null || ImageFile.Length == 0)
    {
        TempData["ErrorMessage"] = "Vui lòng chọn hình ảnh sản phẩm";
        return View(product);
    }
    
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
    var extension = Path.GetExtension(ImageFile.FileName).ToLower();
    
    if (!allowedExtensions.Contains(extension))
    {
        TempData["ErrorMessage"] = "Chỉ chấp nhận file ảnh: JPG, PNG, WebP";
        return View(product);
    }
    
    if (ImageFile.Length > 5 * 1024 * 1024)
    {
        TempData["ErrorMessage"] = "File ảnh không được vượt quá 5MB";
        return View(product);
    }
    
    // ✅ UPLOAD FILE
    try
    {
        var fileName = $"{Guid.NewGuid()}{extension}";
        var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
        
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }
        
        var filePath = Path.Combine(uploadsFolder, fileName);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await ImageFile.CopyToAsync(stream);
        }
        
        product.ImageUrl = $"/images/products/{fileName}";
        Console.WriteLine($"[DEBUG] Image saved: {product.ImageUrl}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Upload failed: {ex.Message}");
        TempData["ErrorMessage"] = $"Lỗi upload: {ex.Message}";
        return View(product);
    }
    
    // ✅ LƯU SẢN PHẨM
    product.Id = InMemoryDataStore.GetNextProductId();
    
    InMemoryDataStore.Products.Add(product);
    
    Console.WriteLine($"[DEBUG] Product added: ID={product.Id}, Name={product.Name}, Price={product.Price}");
    Console.WriteLine($"[DEBUG] Total products: {InMemoryDataStore.Products.Count}");
    
    TempData["SuccessMessage"] = $"Thêm sản phẩm '{product.Name}' thành công!";
    return RedirectToAction(nameof(Index));
}
        public IActionResult Edit(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var p = InMemoryDataStore.Products.FirstOrDefault(x => x.Id == id);
            if (p == null) return NotFound();
            ViewBag.CopyList = InMemoryDataStore.Products
    .Where(x => x.Id != id)
    .Select(x => new { x.Id, x.Name })
    .ToList();
            return View(p);
        }

     [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, IFormFile? ImageFile)
{
    if (!IsAdmin()) return RedirectToAction("Login", "Auth");

    var existing = InMemoryDataStore.Products.FirstOrDefault(x => x.Id == id);
    if (existing == null) return NotFound();

    // ✅ DEBUG - Xem tất cả dữ liệu form
    Console.WriteLine("[DEBUG FORM DATA]");
    foreach (var key in Request.Form.Keys)
    {
        Console.WriteLine($"  {key} = {Request.Form[key]}");
    }

    // ✅ ĐỌC TẤT CẢ DỮ LIỆU TỪ Request.Form
    existing.Name = Request.Form["Name"].ToString();
    existing.Brand = Request.Form["Brand"].ToString();
    existing.Engine = Request.Form["Engine"].ToString();
    existing.Fuel = Request.Form["Fuel"].ToString();
    existing.Badge = Request.Form["Badge"].ToString();
    existing.Color = Request.Form["Color"].ToString();
    existing.Warranty = Request.Form["Warranty"].ToString();
    existing.Description = Request.Form["Description"].ToString();

    // Parse số
    if (decimal.TryParse(Request.Form["Price"], out var price)) 
        existing.Price = price;
    
    if (decimal.TryParse(Request.Form["OldPrice"], out var oldPrice) && oldPrice > 0) 
        existing.OldPrice = oldPrice;
    else
        existing.OldPrice = null;
    
    if (decimal.TryParse(Request.Form["Rating"], out var rating)) 
        existing.Rating = rating;
    
    if (int.TryParse(Request.Form["DiscountPercent"], out var discount) && discount > 0) 
        existing.DiscountPercent = discount;
    else
        existing.DiscountPercent = null;
    
    if (int.TryParse(Request.Form["CategoryId"], out var catId) && catId > 0) 
        existing.CategoryId = catId;
    else
        existing.CategoryId = null;
    
    if (int.TryParse(Request.Form["Stock"], out var stock)) 
        existing.Stock = stock;

    // ✅ DEBUG
    Console.WriteLine($"[DEBUG EDIT] Name={existing.Name}, Brand={existing.Brand}, Price={existing.Price}");

    // ✅ Validate
    if (string.IsNullOrWhiteSpace(existing.Name))
    {
        ModelState.AddModelError("Name", "Vui lòng nhập tên sản phẩm");
        return View(existing);
    }

    if (existing.Price <= 0)
    {
        ModelState.AddModelError("Price", "Giá sản phẩm phải lớn hơn 0");
        return View(existing);
    }

    // ✅ Ảnh mới (tùy chọn)
    if (ImageFile is { Length: > 0 })
    {
        var ext = Path.GetExtension(ImageFile.FileName).ToLower();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        
        if (!allowed.Contains(ext))
        {
            ModelState.AddModelError("", "Ảnh không hợp lệ");
            return View(existing);
        }

        // Xóa ảnh cũ
        if (!string.IsNullOrEmpty(existing.ImageUrl))
        {
            var oldPath = Path.Combine(_env.WebRootPath, existing.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Delete(oldPath);
                Console.WriteLine($"[DEBUG] Deleted old image: {oldPath}");
            }
        }

        // Upload ảnh mới
        var fileName = $"{Guid.NewGuid()}{ext}";
        var folder = Path.Combine(_env.WebRootPath, "images", "products");
        
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        
        var filePath = Path.Combine(folder, fileName);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await ImageFile.CopyToAsync(stream);
        }
        
        existing.ImageUrl = $"/images/products/{fileName}";
        Console.WriteLine($"[DEBUG] New image saved: {existing.ImageUrl}");
    }

    TempData["SuccessMessage"] = $"Cập nhật sản phẩm '{existing.Name}' thành công!";
    return RedirectToAction(nameof(Index));
}        public IActionResult Delete(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var p = InMemoryDataStore.Products.FirstOrDefault(x => x.Id == id);
            if (p == null) return NotFound();
            
            return View(p);
        }
        
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            
            var p = InMemoryDataStore.Products.FirstOrDefault(x => x.Id == id);
            if (p != null)
            {
                // ✅ Xóa file ảnh
                if (!string.IsNullOrEmpty(p.ImageUrl))
                {
                    var imagePath = Path.Combine(_env.WebRootPath, p.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                
                InMemoryDataStore.Products.Remove(p);
                TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}