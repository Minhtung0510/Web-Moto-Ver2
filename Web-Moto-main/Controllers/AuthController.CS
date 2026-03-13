using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MotoBikeStore.Models;
using MotoBikeStore.Services;

namespace MotoBikeStore.Controllers
{
    public class AuthController : Controller
    {
        const string USER_KEY = "CURRENT_USER";

        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password, string? returnUrl = null)
        {
            Console.WriteLine($"[DEBUG] Attempt login: {email}");

            var user = InMemoryDataStore.Users
                .FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                Console.WriteLine("[DEBUG] User not found or inactive / wrong password");
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return View();
            }

            // lưu session 1 chỗ duy nhất
            HttpContext.Session.SetObjectAsJson("CURRENT_USER", new UserSession{
                  Id = user.Id,
    FullName = user.FullName,
    Email = user.Email,
    Role = user.Role
            });
            Console.WriteLine("[DEBUG] Session set OK");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.Role == "Admin"
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Profile");
        }

        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register([Bind("FullName,Email,Phone,Address")] User user,
                              string password, string confirmPassword)
        {
            ModelState.Remove(nameof(user.PasswordHash));

    // Log raw form để chắc chắn có trường Email
    if (Request.HasFormContentType)
    {
        Console.WriteLine($"[DEBUG] Form[Email]='{Request.Form["Email"]}'");
        Console.WriteLine($"[DEBUG] Payload -> FullName='{user.FullName}', Email='{user.Email}'");
    }

    // 1) Chặn ngay nếu model không hợp lệ
    if (!ModelState.IsValid)
    {
        var errs = string.Join(" | ",
            ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        Console.WriteLine("[DEBUG] ModelState INVALID: " + errs);
        return View(user);
    }

    // 2) Các validate còn lại
    if (InMemoryDataStore.Users.Any(u => 
        u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
    {
        ModelState.AddModelError("Email", "Email đã được sử dụng");
        return View(user);
    }
    if (password != confirmPassword)
    {
        ModelState.AddModelError("", "Mật khẩu xác nhận không khớp");
        return View(user);
    }
    if (password.Length < 6)
    {
        ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự");
        return View(user);
    }

    // 3) Lưu user
    user.Id = InMemoryDataStore.Users.Any() ? InMemoryDataStore.Users.Max(u => u.Id) + 1 : 1;
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
    user.Role = "Customer";
    user.IsActive = true;
    user.CreatedAt = DateTime.UtcNow;

    InMemoryDataStore.Users.Add(user);
    Console.WriteLine($"[DEBUG] Registered: {user.Email} (Id={user.Id})");
    Console.WriteLine("[DEBUG] Current users: " + string.Join(", ", InMemoryDataStore.Users.Select(u => u.Email)));

    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
    return RedirectToAction("Login");
}

        public IActionResult Logout()
        {
              HttpContext.Session.Remove("CURRENT_USER");
    return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
              var sess = HttpContext.Session.GetObjectFromJson<UserSession>("CURRENT_USER");
    if (sess == null) return RedirectToAction("Login");

    var user = InMemoryDataStore.Users.FirstOrDefault(u => u.Id == sess.Id);
    if (user == null) return RedirectToAction("Logout");

    return View(user);
        }

        [HttpPost]
        public IActionResult Profile(User updatedUser)
        {
            var current = HttpContext.Session.GetObjectFromJson<dynamic>(USER_KEY);
            if (current == null) return RedirectToAction("Login");

            var user = InMemoryDataStore.Users.FirstOrDefault(u => u.Id == (int)current.Id);
            if (user == null) return RedirectToAction("Logout");

            user.FullName = updatedUser.FullName;
            user.Phone    = updatedUser.Phone;
            user.Address  = updatedUser.Address;

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }
    }
}
