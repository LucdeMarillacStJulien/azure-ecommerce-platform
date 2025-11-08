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

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Role.Any(r => r.Name == "Admin"))
        db.Role.Add(new Role { Name = "Admin" });

    if (!db.Role.Any(r => r.Name == "Customer"))
        db.Role.Add(new Role { Name = "Customer" });

    db.SaveChanges();

    var adminRoleId = db.Role.Where(r => r.Name == "Admin").Select(r => r.RoleId).First();
    var customerRoleId = db.Role.Where(r => r.Name == "Customer").Select(r => r.RoleId).First();


    // 2) Seed Default Admin (for Login)
    if (!db.User.Any(u => u.Email == "admin@system.local"))
    {
        var adminUser = new User
        {
            Email = "admin@system.local",
            Password = "admin123"   // NO HASHING AS REQUIRED
        };
        db.User.Add(adminUser);
        db.SaveChanges();

        db.UserRole.Add(new UserRole { UserId = adminUser.UserId, RoleId = adminRoleId });
        db.SaveChanges();

        db.Admin.Add(new Admin
        {
            UserId = adminUser.UserId,
            FirstName = "System",
            Surname = "Administrator"
        });
        db.SaveChanges();
    }


    // 3) Seed Default Customer (for Login)
    if (!db.User.Any(u => u.Email == "customer@demo.local"))
    {
        var customerUser = new User
        {
            Email = "customer@demo.local",
            Password = "cust123"  // NO HASHING
        };
        db.User.Add(customerUser);
        db.SaveChanges();

        db.UserRole.Add(new UserRole { UserId = customerUser.UserId, RoleId = customerRoleId });
        db.SaveChanges();

        db.Customer.Add(new Customer
        {
            UserId = customerUser.UserId,
            FirstName = "Demo",
            LastName = "Customer",
            DOB = new DateTime(1999, 1, 1),
            PhoneNumber = "0710000000",
            Email = "customer@demo.local",
            Company = "Demo Corp",
            AddressLine1 = "123 Demo Road",
            AddressLine2 = "Unit A",
            City = "Cape Town",
            State = "Western Cape",
            ZipCode = "8001",
            Country = "South Africa"
        });
        db.SaveChanges();
    }
}

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
