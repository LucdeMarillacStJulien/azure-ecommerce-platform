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
using System.Security.Claims;

namespace ST10382638_CLDV_POE.Controllers
{
    /// <summary>
    /// Handles cart functionality including add, update, remove, and display of cart items.
    /// </summary>
    [Authorize]
    public class CartController : Controller
    {
        // Database context dependency injected for data access.
        private readonly AppDbContext _context;

        /// <summary>
        /// Constructor injecting the AppDbContext.
        /// </summary>
        /// <param name="context">Entity Framework Core DbContext instance.</param>
        public CartController(AppDbContext context)
        {
            _context = context;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves the current logged-in user's ID from authentication claims.
        /// </summary>
        /// <returns>Authenticated user's unique ID.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if user not authenticated.</exception>
        private string CurrentUserId()
        {
            // Ensure the user is authenticated before proceeding.
            if (User?.Identity?.IsAuthenticated != true)
                throw new UnauthorizedAccessException();

            // Extract user ID from claims.
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id))
                throw new UnauthorizedAccessException();

            return id;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Displays the user's shopping cart items.
        /// </summary>
        /// <returns>View of current cart items and total cost.</returns>
        public async Task<IActionResult> IndexAsync()
        {
            // Retrieve user ID from session.
            var uid = CurrentUserId();

            // Fetch all cart items for this user ordered by product name.
            var items = await _context.CartItem
                .Where(c => c.UserId == uid)
                .OrderBy(c => c.ProductName)
                .ToListAsync();

            // Calculate total price for all cart items.
            var total = items.Sum(i => i.UnitPrice * i.Quantity);
            ViewData["Total"] = total;

            // Return view displaying cart contents.
            return View(items);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Adds a new product to the cart or updates its quantity if it already exists.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="productName">Name of the product.</param>
        /// <param name="unitPrice">Price per item.</param>
        /// <param name="imageUrl">Optional image URL of the product.</param>
        /// <param name="qty">Quantity to add (default = 1).</param>
        /// <returns>Redirects to customer main view.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string productId, string productName, decimal unitPrice, string? imageUrl, int qty = 1)
        {
            // Validate that productId and quantity are valid.
            if (string.IsNullOrWhiteSpace(productId) || qty < 1)
                return BadRequest();

            // Get the current user's ID.
            var uid = CurrentUserId();

            // Attempt to find existing cart item for this user and product.
            var item = await _context.CartItem.FindAsync(uid, productId);

            // If item not in cart, create new cart entry.
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
                // Increase quantity if product already exists.
                item.Quantity += qty;
                item.UpdatedUtc = DateTime.UtcNow;
            }

            // Commit changes to database.
            await _context.SaveChangesAsync();

            // Redirect back to main customer shop page.
            return RedirectToAction("Main", "Customer");
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Updates the quantity of an existing cart item or removes it if quantity is zero or less.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="qty">New quantity value.</param>
        /// <returns>Redirects to cart index view.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string productId, int qty)
        {
            // Retrieve current user's ID.
            var uid = CurrentUserId();

            // Locate cart item by composite key (UserId + ProductId).
            var item = await _context.CartItem.FindAsync(uid, productId);
            if (item == null) return NotFound();

            // Remove item if quantity is zero or below, otherwise update quantity.
            if (qty <= 0)
                _context.CartItem.Remove(item);
            else
                item.Quantity = qty;

            // Update timestamp for item modification.
            item.UpdatedUtc = DateTime.UtcNow;

            // Save changes to database.
            await _context.SaveChangesAsync();

            // Reload the cart view.
            return RedirectToAction("Index");
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Removes an item completely from the user's cart.
        /// </summary>
        /// <param name="productId">Identifier of the product to remove.</param>
        /// <returns>Redirects to cart index view.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(string productId)
        {
            // Identify the user.
            var uid = CurrentUserId();

            // Locate the cart item to delete.
            var item = await _context.CartItem.FindAsync(uid, productId);
            if (item != null)
            {
                _context.CartItem.Remove(item);
                await _context.SaveChangesAsync();
            }

            // Return to updated cart page.
            return RedirectToAction("Index");
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Returns the total number of items in the cart for the current user.
        /// </summary>
        /// <returns>Item count as plain text.</returns>
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            // If not authenticated, return 0.
            if (!User.Identity?.IsAuthenticated ?? true) return Content("0");

            // Retrieve user ID and count items.
            var uid = CurrentUserId();
            var count = await _context.CartItem.Where(c => c.UserId == uid).CountAsync();

            // Return total as string content.
            return Content(count.ToString());
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
