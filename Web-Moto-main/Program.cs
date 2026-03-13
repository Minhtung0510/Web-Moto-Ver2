using MotoBikeStore.Models;
using MotoBikeStore.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MotoBikeContext>(options =>
    options.UseSqlServer(connectionString));
// ============ SERVICES ============
builder.Services.AddControllersWithViews();

// Session - BẮT BUỘC
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => {
    o.Cookie.Name = ".MotoBikeStore.Session";
    o.IdleTimeout = TimeSpan.FromHours(4);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});


var app = builder.Build();

// ============ MIDDLEWARE ============
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // ⚠️ QUAN TRỌNG
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// ✅ KHỞI TẠO SEASONAL PROMOTIONS
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
   try 
    {
        var context = services.GetRequiredService<MotoBikeContext>();
        // Tự động áp dụng các Migration còn thiếu
        context.Database.Migrate(); 
        // Gọi Seeder đã viết trong MotoBikeContext.cs
        DbSeeder.Seed(context);
    
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi đồng bộ DB: {ex.Message}");
    }
}
app.Run();