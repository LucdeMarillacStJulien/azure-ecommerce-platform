using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10382638_CLDV_POE.Data;
using ST10382638_CLDV_POE.Models;
using ST10382638_CLDV_POE.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ST10382638_CLDV_POE.Controllers
{
    [Authorize(Roles = "Customer, Admin")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly CustomerTableService _tableStorage;
        private readonly ProductTableService _productTable;

        public CustomerController(AppDbContext context, CustomerTableService tableStorage, ProductTableService productTableService)
        {
            _context = context;
            _tableStorage = tableStorage;
            _productTable = productTableService;
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Main()
        {
            var all = await _productTable.GetAllProductsAsync();
            var available = all.Where(p => p.IsAvailable).ToList();
            return View(available);
        }

        public async Task<IActionResult> Shop(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Main));

            var product = await _productTable.GetProductByIdAsync(id);
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int? id)
        {
            var customer = await _context.Customer.ToListAsync();
            return View(customer);
        }

        // ---------- REGISTRATION / CREATE PIPELINE ----------

        [AllowAnonymous]
        public ActionResult Create(string? source = null, string? returnUrl = null)
        {
            var isRegister = string.Equals(source, "Register", StringComparison.OrdinalIgnoreCase);

            // Only Admins may create when NOT in Register flow
            if (!isRegister && !User.IsInRole("Admin"))
                return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();

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
            var isRegister = string.Equals(source, "Register", StringComparison.OrdinalIgnoreCase);

            if (!isRegister && !User.IsInRole("Admin"))
                return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();

            // model cleanup
            ModelState.Remove(nameof(customer.UserId));
            ModelState.Remove(nameof(customer.User));
            ModelState.Remove("User.Password");
            ModelState.Remove("User.Email");

            if (!ModelState.IsValid)
            {
                ViewBag.Source = source;
                ViewBag.ReturnUrl = returnUrl;
                return View("Create", customer);
            }

            TempData["PendingCustomer"] = JsonSerializer.Serialize(customer);
            TempData["Source"] = source ?? "";
            TempData["ReturnUrl"] = returnUrl ?? Url.Action("Login", "Account");
            TempData.Keep();
            return RedirectToAction(nameof(Password));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Password()
        {
            var source = TempData["Source"] as string ?? "";
            var isRegister = string.Equals(source, "Register", StringComparison.OrdinalIgnoreCase);

            if (!isRegister && !User.IsInRole("Admin"))
                return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();

            if (!TempData.TryGetValue("PendingCustomer", out var jsonObj) || jsonObj is null)
                return RedirectToAction(nameof(Create), new { source });

            TempData.Keep();
            return View(new PasswordVm());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Password(PasswordVm vm)
        {
            var source = TempData["Source"] as string ?? "";
            var returnUrl = TempData["ReturnUrl"] as string ?? Url.Action("Login", "Account");
            var isRegister = string.Equals(source, "Register", StringComparison.OrdinalIgnoreCase);

            if (!isRegister && !User.IsInRole("Admin"))
                return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();

            if (!TempData.TryGetValue("PendingCustomer", out var jsonObj))
                return RedirectToAction(nameof(Create), new { source });

            var pendingCustomer = JsonSerializer.Deserialize<Customer>(jsonObj.ToString()!);

            var errors = ValidatePassword(vm.Password, vm.ConfirmPassword);
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            if (!ModelState.IsValid) { TempData.Keep(); return View(vm); }

            // build user from wrapper email
            var user = pendingCustomer!.User ?? new User { Email = pendingCustomer.Email ?? string.Empty };
            user.Password = vm.Password; // TODO: hash

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            var customerRole = await _context.Role.FirstAsync(r => r.Name == "Customer");
            _context.UserRole.Add(new UserRole { UserId = user.UserId, RoleId = customerRole.RoleId });
            await _context.SaveChangesAsync();

            pendingCustomer.UserId = user.UserId;
            pendingCustomer.User = null;

            await _tableStorage.InsertCustomerAsync(pendingCustomer);

            // clear TempData
            TempData.Remove("PendingCustomer");
            TempData.Remove("Source");
            TempData.Remove("ReturnUrl");

            if (isRegister && !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            var returnUrl = Url.Action("Login", "Account");
            return RedirectToAction(nameof(Create), new { source = "Register", returnUrl });
        }

        // ----------------------------------------------------

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
