using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using MotoBikeStore.Services;
using System.Security.Claims;

namespace MotoBikeStore.Controllers
{
    public class AuthController : Controller
    {
        private readonly MotoBikeContext _db;
        public AuthController(MotoBikeContext context)
        {
            _db = context;
        }

        const string USER_KEY = "CURRENT_USER";

        // ── Login (email/password) ────────────────────────────────────────────
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password, string? returnUrl = null)
        {
            Console.WriteLine($"[DEBUG] Attempt login: {email}");

            var user = _db.Users
                .FirstOrDefault(u => u.Email == email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                Console.WriteLine("[DEBUG] User not found or inactive / wrong password");
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return View();
            }

            HttpContext.Session.SetObjectAsJson(USER_KEY, new UserSession
            {
                Id       = user.Id,
                FullName = user.FullName,
                Email    = user.Email,
                Role     = user.Role
            });
            Console.WriteLine("[DEBUG] Session set OK");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.Role == "Admin"
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Profile");
        }

        // ── Google OAuth: khởi động flow ─────────────────────────────────────
        // GET /Auth/ExternalLogin?provider=Google
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(provider))
                return RedirectToAction("Login");

            var redirectUrl = Url.Action("ExternalLoginCallback", "Auth",
                new { provider, returnUrl });

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, provider);
        }

        // ── Google OAuth: nhận callback ───────────────────────────────────────
        // GET /Auth/ExternalLoginCallback?provider=Google
        public async Task<IActionResult> ExternalLoginCallback(string provider, string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync("ExternalCookies");

            if (!result.Succeeded || result.Principal == null)
            {
                TempData["ErrorMessage"] = "Đăng nhập Google thất bại. Vui lòng thử lại.";
                return RedirectToAction("Login");
            }

            // Lấy thông tin từ Google
            var claims     = result.Principal.Claims.ToList();
            var email      = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name       = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var externalId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            // Xoá cookie tạm ngay sau khi đọc xong
            await HttpContext.SignOutAsync("ExternalCookies");

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Tài khoản Google không có email. Vui lòng dùng email/password.";
                return RedirectToAction("Login");
            }

            // Tìm hoặc tạo user trong DB
            var emailLower = email.ToLower();
            var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == emailLower);

            if (user == null)
            {
                // Tạo tài khoản mới từ Google
                user = new User
                {
                    Email            = email,
                    FullName         = name ?? email.Split('@')[0],
                    PasswordHash     = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Role             = "Customer",
                    IsActive         = true,
                    CreatedAt        = DateTime.UtcNow,
                    ExternalProvider = provider,
                    ExternalId       = externalId
                };
                _db.Users.Add(user);
                _db.SaveChanges();

                Console.WriteLine($"[OAUTH] New user created: {email} via {provider}");
                TempData["SuccessMessage"] = $"Chào mừng {user.FullName}! Tài khoản đã được tạo qua {provider}.";
            }
            else
            {
                // User đã có → liên kết Google nếu chưa
                bool changed = false;
                if (user.ExternalProvider == null)
                {
                    user.ExternalProvider = provider;
                    user.ExternalId       = externalId;
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrEmpty(name))
                {
                    user.FullName = name;
                    changed = true;
                }
                if (changed) _db.SaveChanges();

                Console.WriteLine($"[OAUTH] Existing user login: {email} via {provider}");
            }

            if (!user.IsActive)
            {
                TempData["ErrorMessage"] = "Tài khoản đã bị khóa. Vui lòng liên hệ admin.";
                return RedirectToAction("Login");
            }

            // Đặt session (giống login thường)
            HttpContext.Session.SetObjectAsJson(USER_KEY, new UserSession
            {
                Id       = user.Id,
                FullName = user.FullName,
                Email    = user.Email,
                Role     = user.Role
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.Role == "Admin"
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Profile");
        }

        // ── Register ──────────────────────────────────────────────────────────
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register([Bind("FullName,Email,Phone,Address")] User user,
                              string password, string confirmPassword)
        {
            ModelState.Remove(nameof(user.PasswordHash));

            if (Request.HasFormContentType)
            {
                Console.WriteLine($"[DEBUG] Form[Email]='{Request.Form["Email"]}'");
                Console.WriteLine($"[DEBUG] Payload -> FullName='{user.FullName}', Email='{user.Email}'");
            }

            if (!ModelState.IsValid)
            {
                var errs = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                Console.WriteLine("[DEBUG] ModelState INVALID: " + errs);
                return View(user);
            }

            var emailLower = user.Email.ToLower();
            if (_db.Users.Any(u => u.Email.ToLower() == emailLower))
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

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.Role         = "Customer";
            user.IsActive     = true;
            user.CreatedAt    = DateTime.UtcNow;

            _db.Users.Add(user);
            _db.SaveChanges();
            Console.WriteLine($"[DEBUG] Registered: {user.Email} (Id={user.Id})");

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // ── Logout ────────────────────────────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("CURRENT_USER");
            return RedirectToAction("Index", "Home");
        }

        // ── Profile ───────────────────────────────────────────────────────────
        public IActionResult Profile()
        {
            var sess = HttpContext.Session.GetObjectFromJson<UserSession>("CURRENT_USER");
            if (sess == null) return RedirectToAction("Login");

            var user = _db.Users.FirstOrDefault(u => u.Id == sess.Id);
            if (user == null) return RedirectToAction("Logout");

            return View(user);
        }

        [HttpPost]
        public IActionResult Profile(User updatedUser)
        {
            var current = HttpContext.Session.GetObjectFromJson<UserSession>(USER_KEY);
            if (current == null) return RedirectToAction("Login");

            var user = _db.Users.FirstOrDefault(u => u.Id == current.Id);
            if (user == null) return RedirectToAction("Logout");

            user.FullName = updatedUser.FullName;
            user.Phone    = updatedUser.Phone;
            user.Address  = updatedUser.Address;

            _db.SaveChanges();
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }
    }
}