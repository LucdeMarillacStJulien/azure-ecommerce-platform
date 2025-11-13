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

        private static IEnumerable<string> GetAllStatuses()
        {
            return OrderStatus.All;
        }

        // Builds a CustomerId → "First Last" map from SQL for the given string ids
        private async Task<Dictionary<int, string>> BuildCustomerNamesAsync(IEnumerable<string> ids)
        {
            var idInts = ids
                .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .Distinct()
                .ToList();

            if (idInts.Count == 0) return new Dictionary<int, string>();

            // FIX: match on Customer.UserId and key the dictionary by UserId
            return await _context.Customer
                .Where(c => idInts.Contains(c.UserId))
                .Select(c => new { c.UserId, FullName = c.FirstName + " " + c.LastName })
                .ToDictionaryAsync(x => x.UserId, x => x.FullName);
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
                OrderDate = DateTime.UtcNow.AddHours(2),
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
        public async Task<IActionResult> Index(string q = null, string status = null, string customerId = null, DateTime? dateFrom = null, DateTime? dateTo = null, double? minTotal = null,
                                                double? maxTotal = null, int? minItems = null, int? maxItems = null, string sort = "date_desc")
        {
            var all = await _order.GetAllOrdersAsync() ?? new List<Order>();

            // Text search: Order ID (RowKey) only (matches view's placeholder)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                all = all.Where(o =>
                    !string.IsNullOrEmpty(o.RowKey) &&
                    o.RowKey.Contains(term, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Status filter (dropdown)
            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                all = all.Where(o => string.Equals(o.Status, s, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Date range (inclusive end)
            if (dateFrom.HasValue)
                all = all.Where(o => o.OrderDate >= dateFrom.Value).ToList();

            if (dateTo.HasValue)
            {
                var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                all = all.Where(o => o.OrderDate <= end).ToList();
            }

            // Totals range
            if (minTotal.HasValue)
                all = all.Where(o => o.TotalPrice >= minTotal.Value).ToList();

            if (maxTotal.HasValue)
                all = all.Where(o => o.TotalPrice <= maxTotal.Value).ToList();

            // ItemCount range
            if (minItems.HasValue)
                all = all.Where(o => o.ItemCount >= minItems.Value).ToList();

            if (maxItems.HasValue)
                all = all.Where(o => o.ItemCount <= maxItems.Value).ToList();

            // Default ordering: newest first (matches table rendering)
            all = all.OrderByDescending(o => o.OrderDate).ToList();

            // Names + statuses for the view
            var names = await BuildCustomerNamesAsync(all.Select(o => o.CustomerId));
            ViewData["CustomerNames"] = names;
            ViewData["Statuses"] = GetAllStatuses();

            // Preserve chosen filters
            ViewData["q"] = q;
            ViewData["status"] = status;
            ViewData["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
            ViewData["dateTo"] = dateTo?.ToString("yyyy-MM-dd");
            ViewData["minTotal"] = minTotal;
            ViewData["maxTotal"] = maxTotal;
            ViewData["minItems"] = minItems;
            ViewData["maxItems"] = maxItems;

            return View(all);
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminDetails(string id, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            var entity = await _order.GetOrderByIdAsync(id);
            if (entity == null) return NotFound();

            // unified: provide dictionary even for one id
            ViewData["CustomerNames"] = await BuildCustomerNamesAsync(new[] { entity.CustomerId });
            ViewData["Statuses"] = GetAllStatuses();

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

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> MyOrders(string q = null, string status = null, DateTime? dateFrom = null, DateTime? dateTo = null, double? minTotal = null,
            double? maxTotal = null, int? minItems = null, int? maxItems = null)
        {
            var uid = CurrentUserId();
            var all = await _order.GetAllOrdersAsync() ?? new List<Order>();
            var mine = all.Where(o => o.CustomerId == uid).ToList();

            // Text search: Order ID only (to mirror placeholder; extend if you add more fields)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                mine = mine.Where(o =>
                    !string.IsNullOrEmpty(o.RowKey) &&
                    o.RowKey.Contains(term, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                mine = mine.Where(o => string.Equals(o.Status, s, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Date range (inclusive end)
            if (dateFrom.HasValue)
                mine = mine.Where(o => o.OrderDate >= dateFrom.Value).ToList();

            if (dateTo.HasValue)
            {
                var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                mine = mine.Where(o => o.OrderDate <= end).ToList();
            }

            // Totals
            if (minTotal.HasValue)
                mine = mine.Where(o => o.TotalPrice >= minTotal.Value).ToList();
            if (maxTotal.HasValue)
                mine = mine.Where(o => o.TotalPrice <= maxTotal.Value).ToList();

            // Item counts
            if (minItems.HasValue)
                mine = mine.Where(o => o.ItemCount >= minItems.Value).ToList();
            if (maxItems.HasValue)
                mine = mine.Where(o => o.ItemCount <= maxItems.Value).ToList();

            // Default ordering: newest first
            mine = mine.OrderByDescending(o => o.OrderDate).ToList();

            // Names/status list for the view (kept consistent with your other views)
            ViewData["CustomerNames"] = await BuildCustomerNamesAsync(mine.Select(o => o.CustomerId));
            ViewData["Statuses"] = GetAllStatuses();

            // Preserve chosen filters
            ViewData["q"] = q;
            ViewData["status"] = status;
            ViewData["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
            ViewData["dateTo"] = dateTo?.ToString("yyyy-MM-dd");
            ViewData["minTotal"] = minTotal;
            ViewData["maxTotal"] = maxTotal;
            ViewData["minItems"] = minItems;
            ViewData["maxItems"] = maxItems;

            ViewData["Title"] = "My Orders";
            return View("MyOrders", mine);
        }



        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> Details(string id, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var entity = await _order.GetOrderByIdAsync(id);
            if (entity == null)
                return NotFound();

            var uid = CurrentUserId();
            if (!string.Equals(entity.CustomerId, uid, StringComparison.Ordinal))
                return Forbid();

            // unified: provide dictionary for this order's id
            ViewData["CustomerNames"] = await BuildCustomerNamesAsync(new[] { entity.CustomerId });

            ViewData["Title"] = $"Order {entity.RowKey}";
            ViewData["returnUrl"] = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action("MyOrders");

            return View("Details", entity);
        }

    }
}
