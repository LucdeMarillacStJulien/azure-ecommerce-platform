using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10382638_CLDV_POE.Data;
using ST10382638_CLDV_POE.Models;
using ST10382638_CLDV_POE.Services;
using System.Security.Claims;
using System.Text.Json;

namespace ST10382638_CLDV_POE.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly QueueStorageService _queue;
        private readonly OrderTableService _order;

        public OrderController(AppDbContext context, QueueStorageService queue, OrderTableService order)
        {
            _context = context;
            _queue = queue;
            _order = order;
        }

        private string CurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(id))
                throw new UnauthorizedAccessException();

            return id;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var uid = CurrentUserId();

            // Load cart
            var items = await _context.CartItem
                .Where(c => c.UserId == uid)
                .OrderBy(c => c.ProductName)
                .ToListAsync();

            if (items.Count == 0)
                return BadRequest("Cart is empty.");

            // Serialize full cart snapshot for storage in Azure Table as a single column
            var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = null }; // keep PascalCase to match model
            var itemsJson = JsonSerializer.Serialize(items, jsonOpts);

            // Generate numeric RowKey for easy input (uses your existing service)
            var rowKey = _order.GetNextRowKey();

            // Compute totals from cart (CartItem.UnitPrice is decimal)
            var total = items.Sum(i => i.UnitPrice * i.Quantity);

            // Build Order entity
            var order = new Order
            {
                PartitionKey = "Order",
                RowKey = rowKey,
                CustomerId = uid,



                TotalPrice = (double)total,
                OrderDate = DateTime.UtcNow,
                Status = "Placed",

                // New fields
                ItemsJson = itemsJson,
                ItemCount = items.Count
            };

            await _queue.SendMessageAsync(order);

            // Clear cart after enqueue
            _context.CartItem.RemoveRange(items);
            await _context.SaveChangesAsync();

            // Redirect to a simple confirmation (create a view if you want)
            TempData["OrderPlaced"] = true;
            TempData["OrderId"] = rowKey;

            // TODO: adjust target to your actual Customer Main action/controller
            return RedirectToAction("Main", "Customer", new { orderSuccess = 1, orderId = rowKey });
        }

        [HttpGet]
        public IActionResult Confirmation()
        {
            ViewData["OrderId"] = TempData["OrderId"] as string ?? "";
            return View(); // Views/Order/Confirmation.cshtml (optional simple page)
        }

        [Authorize(Roles = ("Admin"))]
        [HttpGet]
        public async Task<IActionResult> Index(string q = null, string status = null)
        {
            var all = await _order.GetAllOrdersAsync() ?? new List<Order>();

            // Filter: OrderId / CustomerId / Status
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                all = all.Where(o =>
                    (!string.IsNullOrEmpty(o.RowKey) && o.RowKey.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.CustomerId) && o.CustomerId.Contains(term, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                all = all.Where(o => string.Equals(o.Status, s, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Pull customer names from SQL using the CustomerId stored on orders
            var idInts = all.Select(o => int.TryParse(o.CustomerId, out var n) ? n : (int?)null)
                            .Where(n => n.HasValue)
                            .Select(n => n!.Value)
                            .Distinct()
                            .ToList();

            var names = await _context.Customer
                .Where(c => idInts.Contains(c.CustomerId))
                .Select(c => new { c.CustomerId, FullName = (c.FirstName + " " + c.LastName) })
                .ToDictionaryAsync(x => x.CustomerId, x => x.FullName);

            // Make names available to the view without creating a new model
            ViewData["CustomerNames"] = names;

            return View(all.OrderByDescending(o => o.OrderDate).ToList());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminDetails(string id, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            var entity = await _order.GetOrderByIdAsync(id);
            if (entity == null) return NotFound();

            var fallback = Url.Action(nameof(AdminDetails), new { id });
            ViewData["returnUrl"] =
                !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : fallback;

            return View(entity);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, string status, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(status))
                return BadRequest();

            var entity = await _order.GetOrderByIdAsync(id);
            if (entity == null)
                return NotFound();

            entity.Status = status.Trim();
            await _order.UpdateOrderAsync(entity);

            // ✅ Stay on the same view if returnUrl is valid
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // ✅ Otherwise explicitly stay on AdminDetails
            return RedirectToAction("AdminDetails", new { id });
        }

    }
}
