using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using ST10382638_CLDV_POE.Data;
using ST10382638_CLDV_POE.Models;
using ST10382638_CLDV_POE.Services;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ST10382638_CLDV_POE.Controllers
{

    [Authorize(Roles = "Customer, Admin")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly CustomerTableService _tableStorage;

        public CustomerController(AppDbContext context, CustomerTableService tableStorage)
        {
            _context = context;
            _tableStorage = tableStorage;
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Main()
        {

            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int? id)
        {
            var customer = await _context.Customer.ToListAsync();
            return View(customer);
        }

        [AllowAnonymous]
        public ActionResult Create(string? source = null, string? returnUrl = null)
        {
            var isRegister = string.Equals(source, "Register", StringComparison.OrdinalIgnoreCase);
            if(!User.IsInRole("Admin") && isRegister)
                return Forbid();

            ViewBag.Source = source;
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action("Login", "Account")
                : returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Next(Customer customer, string? source, string? returnUrl)
        {


            ModelState.Remove(nameof(customer.UserId));
            ModelState.Remove(nameof(customer.User));
            ModelState.Remove("User.Password");
            ModelState.Remove("User.Email");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model not valid");
                return View("Create", customer);
            }


            TempData["PendingCustomer"] = JsonSerializer.Serialize(customer);
            TempData.Keep("PendingCustomer");
            return RedirectToAction(nameof(Password));
        }

        [HttpGet]
        public IActionResult Password()
        {
            if (!TempData.TryGetValue("PendingCustomer", out var jsonObj) || jsonObj is null)
                return RedirectToAction(nameof(Create));

            TempData.Keep("PendingCustomer");
            TempData.Keep("Source");
            TempData.Keep("ReturnUrl");
            return View(new PasswordVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Password(PasswordVm vm)
        {
            if (!TempData.TryGetValue("PendingCustomer", out var jsonObj) || jsonObj is null)
                return RedirectToAction(nameof(Create));

            var pendingCustomer = JsonSerializer.Deserialize<Customer>(jsonObj.ToString()!);

            var errors = ValidatePassword(vm.Password, vm.ConfirmPassword);
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            if (!ModelState.IsValid) { TempData.Keep("PendingCustomer"); return View(vm); }

            // build user from wrapper email
            var user = pendingCustomer.User ?? new User();
            user.Email = pendingCustomer.Email ?? string.Empty;

            // ✅ set password (hash if you have a hasher)
            user.Password = vm.Password;                     // or Hash(vm.Password)

            _context.User.Add(user);
            await _context.SaveChangesAsync();               // user.UserId generated

            // role link
            var customerRole = await _context.Role.FirstAsync(r => r.Name == "Customer");
            _context.UserRole.Add(new UserRole { UserId = user.UserId, RoleId = customerRole.RoleId });
            await _context.SaveChangesAsync();

            // set FK and avoid duplicate user insert
            pendingCustomer.UserId = user.UserId;
            pendingCustomer.User = null;

            // if you also mirror to Table Storage
            await _tableStorage.InsertCustomerAsync(pendingCustomer);

            var source = TempData["Source"] as string ?? "";
            var returnUrl = TempData["ReturnUrl"] as string ?? "";

            TempData.Remove("PendingCustomer");
            TempData.Remove("Source");
            TempData.Remove("ReturnUrl");


            if (string.Equals(source, "Register", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }


        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        private static List<string> ValidatePassword(string? pwd, string? confirm)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(pwd)) { errors.Add("Password is required."); return errors; }
            if (pwd != confirm) errors.Add("Passwords do not match.");
            if (pwd.Length < 8) errors.Add("At least 8 characters.");
            if (!Regex.IsMatch(pwd, "[A-Z]")) errors.Add("At least one uppercase letter.");
            if (!Regex.IsMatch(pwd, "[a-z]")) errors.Add("At least one lowercase letter.");
            if (!Regex.IsMatch(pwd, "[0-9]")) errors.Add("At least one digit.");
            if (!Regex.IsMatch(pwd, "[^a-zA-Z0-9]")) errors.Add("At least one special character.");
            return errors;
        }
    }
}
