// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// https://chatgpt.com/c/690f7551-669c-8329-9b07-041233d40273
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap5/index.php
// https://www.w3schools.com/js/default.asp

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10382638_CLDV_POE.Data;
using System.Security.Claims;

namespace ST10382638_CLDV_POE.Controllers
{
    /// <summary>
    /// Handles user authentication (login/logout) and role-based sign-in.
    /// </summary>
    public class AccountController : Controller
    {
        // Injected Entity Framework database context for user and cart access.
        private readonly AppDbContext _context;

        /// <summary>
        /// Constructor injection of the application DbContext.
        /// </summary>
        /// <param name="context">EF Core DbContext instance.</param>
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Displays the login page (GET).
        /// </summary>
        /// <returns>Login view.</returns>
        [HttpGet, AllowAnonymous]
        public IActionResult Login()
        {
            // Simply returns the login view to the user.
            return View();
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Processes login submissions (POST) with anti-forgery validation.
        /// </summary>
        /// <param name="email">User email address.</param>
        /// <param name="password">User plaintext password.</param>
        /// <returns>Redirects on success, reloads view on failure.</returns>
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Look up the user including their roles; verify email/password match.
            var user = await _context.User
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            // If no user found or credentials invalid, return to view with model error.
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View();
            }

            // Ensure any existing cookie-auth session is cleared before signing in.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Handle anonymous cart migration: move items from cookie-bound userId to real userId.
            var uid = user.UserId.ToString();
            var anon = Request.Cookies["CartId"];
            if (!string.IsNullOrEmpty(anon))
            {
                // Fetch all cart items tied to the anonymous id and reassign to the authenticated user id.
                var anonItems = await _context.CartItem.Where(c => c.UserId == anon).ToListAsync();
                foreach (var it in anonItems) it.UserId = uid;
                await _context.SaveChangesAsync();
                // Remove the anonymous cart cookie now that items are migrated.
                Response.Cookies.Delete("CartId");
            }

            // Existing login logic
            // Build core identity claims: NameIdentifier and Email for the user.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
            };

            // Add a ClaimTypes.Role claim for each assigned role.
            foreach (var r in user.UserRoles.Select(ur => ur.Role.Name))
                claims.Add(new Claim(ClaimTypes.Role, r));

            // Create the identity and principal for cookie authentication.
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in with the cookie authentication scheme.
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Redirect by role: Admins go to Home/Index, others go to Customer/Main.
            if (claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin"))
                return RedirectToAction("Index", "Home");

            return RedirectToAction("Main", "Customer");
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Logs the current user out and redirects to Login.
        /// </summary>
        /// <returns>Redirect to login page.</returns>
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Clear the authentication cookie/session then go back to the login screen.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
