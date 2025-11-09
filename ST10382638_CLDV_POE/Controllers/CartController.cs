using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10382638_CLDV_POE.Data;
using ST10382638_CLDV_POE.Models;
using System.Security.Claims;

namespace ST10382638_CLDV_POE.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId()
        {
            if (User?.Identity?.IsAuthenticated != true)
                throw new UnauthorizedAccessException();

            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id))
                throw new UnauthorizedAccessException();

            return id;
        }

        public async Task<IActionResult> IndexAsync()
        {
            var uid = CurrentUserId();
            var items = await _context.CartItem
                .Where(c => c.UserId == uid)
                .OrderBy(c => c.ProductName)
                .ToListAsync();

            var total = items.Sum(i => i.UnitPrice * i.Quantity);
            ViewData["Total"] = total;
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string productId, string productName, decimal unitPrice, string? imageUrl, int qty = 1)
        {
            if(string.IsNullOrWhiteSpace(productId) || qty < 1)
                return BadRequest();

            var uid = CurrentUserId();
            var item = await _context.CartItem.FindAsync(uid, productId);
            if (item == null)
            {
                item = new CartItem
                {
                    UserId = uid,
                    ProductId = productId,
                    ProductName = productName,
                    UnitPrice = unitPrice,
                    ImageUrl = imageUrl,
                    Quantity = qty
                };
                _context.CartItem.Add(item);
            }
            else
            {
                item.Quantity += qty;
                item.UpdatedUtc = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Main", "Customer");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string productId, int qty)
        {
            var uid = CurrentUserId();
            var item = await _context.CartItem.FindAsync(uid, productId);
            if (item == null) return NotFound();

            if (qty <= 0)
                _context.CartItem.Remove(item);
            else
                item.Quantity = qty;

            item.UpdatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(string productId)
        {
            var uid = CurrentUserId();
            var item = await _context.CartItem.FindAsync(uid, productId);
            if (item != null)
            {
                _context.CartItem.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Content("0");
            var uid = CurrentUserId();
            var count = await _context.CartItem.Where(c => c.UserId == uid).CountAsync();
            return Content(count.ToString());
        }
    }
}
