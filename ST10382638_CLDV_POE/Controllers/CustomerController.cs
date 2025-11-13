// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// https://chatgpt.com/c/690f7551-669c-8329-9b07-041233d40273
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap5/index.php
// https://www.w3schools.com/js/default.asp

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
    /// <summary>
    /// Customer-facing and admin-facing endpoints for product browsing and customer management.
    /// Handles registration (multi-step), validation, and persistence to EF Core and Table Storage.
    /// </summary>
    [Authorize(Roles = "Customer, Admin")]
    public class CustomerController : Controller
    {
        // EF Core DbContext for relational entities (User, Role, Customer, etc.).
        private readonly AppDbContext _context;
        // Azure Table Storage service for Customer persistence.
        private readonly CustomerTableService _tableStorage;
        // Azure Table Storage service for Products.
        private readonly ProductTableService _productTable;

        /// <summary>
        /// Constructor injection for context and storage services.
        /// </summary>
        public CustomerController(AppDbContext context, CustomerTableService tableStorage, ProductTableService productTableService)
        {
            _context = context;
            _tableStorage = tableStorage;
            _productTable = productTableService;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Customer-only: Show available products, with optional filters for name and category.
        /// </summary>
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Main(string? name = null, string? category = null)
        {
            // Fetch all products from table storage, then filter to IsAvailable.
            var all = await _productTable.GetAllProductsAsync();
            var available = all.Where(p => p.IsAvailable).ToList();

            // Filter by product name (ProductName) if a search term was provided.
            if (!string.IsNullOrWhiteSpace(name))
            {
                var term = name.Trim();
                available = available
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProductName) &&
                                p.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Filter by category (Category) if a category was selected.
            if (!string.IsNullOrWhiteSpace(category))
            {
                available = available
                    .Where(p => !string.IsNullOrWhiteSpace(p.Category) &&
                                string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Provide category list for the dropdown in the view.
            ViewBag.Categories = ProductCategories.List;

            return View(available);
        }


        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// View a single product in the shop by id; fallback to Main if id invalid.
        /// </summary>
        public async Task<IActionResult> Shop(string id)
        {
            // Guard: if no id provided, redirect to product list.
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Main));

            // Load product details for display.
            var product = await _productTable.GetProductByIdAsync(id);
            return View(product);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Admin-only: List all customers from EF Core.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int? id)
        {
            // Load all customers for admin view.
            var customer = await _context.Customer.ToListAsync();
            return View(customer);
        }

        // ---------- REGISTRATION / CREATE PIPELINE ----------

        /// <summary>
        /// Start of registration/create flow. Anonymous allowed for Register; Admin required otherwise.
        /// </summary>
        [AllowAnonymous]
        public ActionResult Create(string? source = null, string? returnUrl = null)
        {
            // Determine if request is a self-registration.
            var isRegister = string.Equals(source, "Register", StringComparison.OrdinalIgnoreCase);

            // Only Admins may create when NOT in Register flow; challenge/forbid accordingly.
            if (!isRegister && !User.IsInRole("Admin"))
                return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();

            // Persist source and return target for next steps.
            ViewBag.Source = source;
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action("Login", "Account")
                : returnUrl;

            return View();
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Validate and stage Customer info into TempData before password step.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Next(Customer customer, string? source, string? returnUrl)
        {
            // Registration vs Admin-create check.
            var isRegister = string.Equals(source, "Register", StringComparison.OrdinalIgnoreCase);

            if (!isRegister && !User.IsInRole("Admin"))
                return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();

            // Model cleanup: remove navigation/derived fields that aren't posted.
            ModelState.Remove(nameof(customer.UserId));
            ModelState.Remove(nameof(customer.User));
            ModelState.Remove("User.Password");
            ModelState.Remove("User.Email");

            // If validation fails, return to Create view with same state.
            if (!ModelState.IsValid)
            {
                ViewBag.Source = source;
                ViewBag.ReturnUrl = returnUrl;
                return View("Create", customer);
            }

            // Stage the customer in TempData to continue the multi-step flow.
            TempData["PendingCustomer"] = JsonSerializer.Serialize(customer);
            TempData["Source"] = source ?? "";
            TempData["ReturnUrl"] = returnUrl ?? Url.Action("Login", "Account");
            TempData.Keep();
            return RedirectToAction(nameof(Password));
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Render password step; ensure staged data exists and authorization matches flow.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Password()
        {
            var source = TempData["Source"] as string ?? "";
            var isRegister = string.Equals(source, "Register", StringComparison.OrdinalIgnoreCase);

            // Enforce Admin for non-register flow.
            if (!isRegister && !User.IsInRole("Admin"))
                return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();

            // Ensure we still have staged customer data; otherwise restart.
            if (!TempData.TryGetValue("PendingCustomer", out var jsonObj) || jsonObj is null)
                return RedirectToAction(nameof(Create), new { source });

            TempData.Keep();
            return View(new PasswordVm());
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Complete password step: validate password rules, create User, assign role, and persist Customer to Table Storage.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Password(PasswordVm vm)
        {
            var source = TempData["Source"] as string ?? "";
            var returnUrl = TempData["ReturnUrl"] as string ?? Url.Action("Login", "Account");
            var isRegister = string.Equals(source, "Register", StringComparison.OrdinalIgnoreCase);

            // Enforce Admin for non-register flow.
            if (!isRegister && !User.IsInRole("Admin"))
                return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();

            // Ensure staged customer exists; otherwise restart.
            if (!TempData.TryGetValue("PendingCustomer", out var jsonObj))
                return RedirectToAction(nameof(Create), new { source });

            // Rehydrate staged Customer.
            var pendingCustomer = JsonSerializer.Deserialize<Customer>(jsonObj.ToString()!);

            // Validate password and collect errors into ModelState.
            var errors = ValidatePassword(vm.Password, vm.ConfirmPassword);
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            if (!ModelState.IsValid) { TempData.Keep(); return View(vm); }

            // Build or update User from pending customer's email (password to be hashed later).
            var user = pendingCustomer!.User ?? new User { Email = pendingCustomer.Email ?? string.Empty };
            user.Password = vm.Password; // TODO: hash

            // Persist User.
            _context.User.Add(user);
            await _context.SaveChangesAsync();

            // Assign "Customer" role.
            var customerRole = await _context.Role.FirstAsync(r => r.Name == "Customer");
            _context.UserRole.Add(new UserRole { UserId = user.UserId, RoleId = customerRole.RoleId });
            await _context.SaveChangesAsync();

            // Link Customer to newly created User and clear navigation to avoid duplicate tracking.
            pendingCustomer.UserId = user.UserId;
            pendingCustomer.User = null;

            // Persist Customer to Azure Table Storage.
            await _tableStorage.InsertCustomerAsync(pendingCustomer);

            // Clear staged flow state.
            TempData.Remove("PendingCustomer");
            TempData.Remove("Source");
            TempData.Remove("ReturnUrl");

            // On self-register, return to provided local URL; otherwise go to admin index.
            if (isRegister && !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Convenience redirector for Registration entry point.
        /// </summary>
        [AllowAnonymous]
        public IActionResult Register()
        {
            // Redirect into Create with source flag and a return target to Login.
            var returnUrl = Url.Action("Login", "Account");
            return RedirectToAction(nameof(Create), new { source = "Register", returnUrl });
        }

        // ----------------------------------------------------

        /// <summary>
        /// Validates password complexity and confirmation; returns list of error messages.
        /// </summary>
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

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Admin: View details of a specific customer, including linked User.
        /// </summary>
        // GET: /Customer/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            // Load customer with navigation to User for display.
            var customer = await _context.Customer
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();
            return View(customer);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Admin: Render edit page for a specific customer, including User.
        /// </summary>
        // GET: /Customer/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            // Load customer and associated user for editing.
            var customer = await _context.Customer
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();
            return View(customer);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Admin: Persist edits to a customer; supports optional password update via form.User.Password.
        /// </summary>
        // POST: /Customer/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer form)
        {
            // Route/customer id mismatch guard.
            if (id != form.CustomerId) return BadRequest();

            // Load tracked entity for update operations.
            var customer = await _context.Customer
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            // Ignore non-posted navigation props to avoid validation noise.
            ModelState.Remove(nameof(form.User));
            ModelState.Remove("User.Email");
            ModelState.Remove("User.Password");

            // Optional debug: log invalid model (view will still receive the model below).
            if (!ModelState.IsValid)
                Console.WriteLine("Model not valid");

            // Map scalar fields from form to entity.
            customer.FirstName = form.FirstName;
            customer.LastName = form.LastName;
            customer.DOB = form.DOB;
            customer.PhoneNumber = form.PhoneNumber;
            customer.Company = form.Company;
            customer.AddressLine1 = form.AddressLine1;
            customer.AddressLine2 = form.AddressLine2;
            customer.City = form.City;
            customer.State = form.State;
            customer.ZipCode = form.ZipCode;
            customer.Country = form.Country;

            // Email is proxied via Customer.Email to underlying User.Email (per your model).
            customer.Email = form.Email;

            // If admin provided a new password, update associated user password.
            if (form.User != null && !string.IsNullOrWhiteSpace(form.User.Password))
            {
                if (customer.User == null) customer.User = new User();
                customer.User.Password = form.User.Password;
            }

            // Final server-side validation before save.
            if (!TryValidateModel(customer))
            {
                return View(customer);
            }

            // Commit changes.
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Admin: Delete a customer (and associated User record). Called via AJAX; returns 200 OK on success.
        /// </summary>
        // POST: /Customer/Delete
        // Called by delete.js via AJAX, uses anti-forgery, returns 200 OK on success.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Load target with user for cleanup.
            var customer = await _context.Customer
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound("Customer not found.");

            // Remove Customer and linked User explicitly.
            _context.Customer.Remove(customer);
            _context.User.Remove(customer.User!);

            await _context.SaveChangesAsync();

            // AJAX success response.
            return Ok(); // delete.js expects success without redirect
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
