using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10382638_CLDV_POE.Data;
using System.Security.Claims;

namespace ST10382638_CLDV_POE.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.User
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View();
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var uid = user.UserId.ToString();
            var anon = Request.Cookies["CartId"];
            if (!string.IsNullOrEmpty(anon))
            {
                var anonItems = await _context.CartItem.Where(c => c.UserId == anon).ToListAsync();
                foreach (var it in anonItems) it.UserId = uid;
                await _context.SaveChangesAsync();
                Response.Cookies.Delete("CartId");
            }

            // Existing login logic
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
            };
            foreach (var r in user.UserRoles.Select(ur => ur.Role.Name))
                claims.Add(new Claim(ClaimTypes.Role, r));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Redirect by role
            if (claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin"))
                return RedirectToAction("Index", "Home");

            return RedirectToAction("Main", "Customer");
        }


        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
