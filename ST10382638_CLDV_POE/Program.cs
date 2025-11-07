using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ST10382638_CLDV_POE.Data;
using ST10382638_CLDV_POE.Models;
using ST10382638_CLDV_POE.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ProductTableService>();
builder.Services.AddSingleton<CustomerTableService>();
builder.Services.AddSingleton<OrderTableService>();
builder.Services.AddSingleton<ProductBlob>();
builder.Services.AddSingleton<ContractService>();
builder.Services.AddSingleton<QueueStorageService>();

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.SlidingExpiration = true;
        o.Cookie.Name = "lcm_auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("CustomerOnly", p => p.RequireRole("Customer"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if(!db.Role.Any(r => r.Name == "Admin"))
        db.Role.Add(new Role { Name = "Admin" });

    if(!db.Role.Any(r => r.Name == "Customer"))
        db.Role.Add(new Role { Name = "Customer" });

    db.SaveChanges();

    var adminEmail = "admin@admin.com";
    if(!db.User.Any(u => u.Email == adminEmail))
    {
        var adminUser = new User
        {
            Email = adminEmail,
            Password = "Admin"
        };
        db.User.Add(adminUser);
        db.SaveChanges();

        var adminRole = db.Role.First(r => r.Name == "Admin");
        db.UserRole.Add(new UserRole
        {
            UserId = adminUser.UserId,
            RoleId = adminRole.RoleId
        });
        db.SaveChanges();
    }
}

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
