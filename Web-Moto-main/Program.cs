using MotoBikeStore.Models;
using MotoBikeStore.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MotoBikeContext>(options =>
    options.UseSqlServer(connectionString));

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpClient();


// ── Session ───────────────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".MotoBikeStore.Session";
    o.IdleTimeout = TimeSpan.FromHours(4);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// ── OAuth Authentication ──────────────────────────────────────────────────────
// Dùng scheme "ExternalCookies" làm cookie tạm để nhận callback từ OAuth provider.
// Sau khi lấy được claims ta xoá cookie này và chuyển sang session tự quản lý.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "ExternalCookies";
})
.AddCookie("ExternalCookies", options =>
{
    options.Cookie.Name = ".MotoBike.External";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.LoginPath = "/Auth/Login";
})
.AddGoogle("Google", options =>
{
    options.SignInScheme = "ExternalCookies";
    options.ClientId     = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/signin-google"; // phải khớp Redirect URI trong Google Console
    // Lấy thêm avatar (tùy chọn)
    options.Scope.Add("profile");
    options.SaveTokens = false;
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();       // ⚠️ Session trước Authentication
app.UseAuthentication(); // ⚠️ Phải có để OAuth hoạt động
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── Seed DB ───────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<MotoBikeContext>();
        context.Database.Migrate();
        DbSeeder.Seed(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi đồng bộ DB: {ex.Message}");
    }
}

app.Run();