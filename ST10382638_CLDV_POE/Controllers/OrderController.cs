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
using System.Security.Claims;
using System.Text.Json;

namespace ST10382638_CLDV_POE.Controllers
{
    /// <summary>
    /// Manages order creation (checkout), admin order listing and status updates,
    /// and customer-facing order views using Queue and Table storage services.
    /// </summary>
    public class OrderController : Controller
    {
        // EF Core DbContext for relational entities (CartItem, Customer names, etc.).
        private readonly AppDbContext _context;
        // Azure Queue Storage service for enqueuing new orders.
        private readonly QueueStorageService _queue;
        // Azure Table Storage service for persisting and querying orders.
        private readonly OrderTableService _order;
        // Azure Table Storage service for product inventory (stock).
        private readonly ProductTableService _productTable;

        /// <summary>
        /// Constructor injection for DbContext and storage services.
        /// </summary>
        public OrderController(AppDbContext context, QueueStorageService queue, OrderTableService order, ProductTableService productTable)
        {
            _context = context;
            _queue = queue;
            _order = order;
            _productTable = productTable;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves the authenticated user's ID from claims; throws if not present.
        /// </summary>
        private string CurrentUserId()
        {
            // Extract user id from NameIdentifier claim and validate.
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(id))
                throw new UnauthorizedAccessException();

            return id;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Helper to expose all valid order statuses for views (dropdowns, etc.).
        /// </summary>
        private static IEnumerable<string> GetAllStatuses()
        {
            return OrderStatus.All;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Builds a map of CustomerId (UserId) to "First Last" using SQL, for provided string ids.
        /// </summary>
        private async Task<Dictionary<int, string>> BuildCustomerNamesAsync(IEnumerable<string> ids)
        {
            // Convert string ids to distinct ints and guard empty set.
            var idInts = ids
                .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .Distinct()
                .ToList();

            if (idInts.Count == 0) return new Dictionary<int, string>();

            // Query EF for customers whose UserId is in the provided set; return dictionary keyed by UserId.
            return await _context.Customer
                .Where(c => idInts.Contains(c.UserId))
                .Select(c => new { c.UserId, FullName = c.FirstName + " " + c.LastName })
                .ToDictionaryAsync(x => x.UserId, x => x.FullName);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Converts the current user's cart into an Order: serializes items, enqueues Order, and clears cart.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var uid = CurrentUserId();

            // Load all cart items for the user and order by product name for deterministic snapshot.
            var items = await _context.CartItem
                .Where(c => c.UserId == uid)
                .OrderBy(c => c.ProductName)
                .ToListAsync();

            // Guard: cannot checkout with an empty cart.
            if (items.Count == 0)
                return BadRequest("Cart is empty.");

            // Attempt to reserve stock for each item in the cart.
            // If any item does not have enough stock, no stock is changed and checkout is blocked.
            var stockOk = await _productTable.TryReserveStockForItemsAsync(items);
            if (!stockOk)
            {
                // Optional: surface a user-facing message via TempData.
                TempData["OrderError"] = "One or more items in your cart do not have enough stock.";
                return RedirectToAction("Index", "Cart");
            }

            // Serialize cart to JSON (preserve PascalCase to match model properties).
            var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = null };
            var itemsJson = JsonSerializer.Serialize(items, jsonOpts);

            // Generate next numeric RowKey for the new order (easy to type/search).
            var rowKey = _order.GetNextRowKey();

            // Calculate total from item unit prices and quantities.
            var total = items.Sum(i => i.UnitPrice * i.Quantity);

            // Build the order entity that the queue-triggered Function will persist to Table Storage.
            var order = new Order
            {
                PartitionKey = "Order",
                RowKey = rowKey,
                CustomerId = uid,

                TotalPrice = (double)total,
                OrderDate = DateTime.UtcNow.AddHours(2), // store in local (UTC+2) if required by app
                Status = "Placed",

                // Snapshot of items and count at time of checkout.
                ItemsJson = itemsJson,
                ItemCount = items.Count
            };

            // Enqueue the order for asynchronous processing by the Function App.
            await _queue.SendMessageAsync(order);

            // Clear the user's cart after enqueue to prevent duplicate processing.
            _context.CartItem.RemoveRange(items);
            await _context.SaveChangesAsync();

            // Provide a simple success signal and id for subsequent views.
            TempData["OrderPlaced"] = true;
            TempData["OrderId"] = rowKey;

            // Redirect back to Customer/Main with success indicators.
            return RedirectToAction("Main", "Customer", new { orderSuccess = 1, orderId = rowKey });
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Optional confirmation view that can display the last OrderId from TempData.
        /// </summary>
        [HttpGet]
        public IActionResult Confirmation()
        {
            // Pass OrderId from TempData to the view, if available.
            ViewData["OrderId"] = TempData["OrderId"] as string ?? "";
            return View(); // Views/Order/Confirmation.cshtml
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Admin: Advanced filterable list of all orders with preserved form inputs for the view.
        /// </summary>
        [Authorize(Roles = ("Admin"))]
        [HttpGet]
        public async Task<IActionResult> Index(string q = null, string status = null, string customerId = null, DateTime? dateFrom = null, DateTime? dateTo = null, double? minTotal = null,
                                                double? maxTotal = null, int? minItems = null, int? maxItems = null, string sort = "date_desc")
        {
            // Load all orders from Table Storage service.
            var all = await _order.GetAllOrdersAsync() ?? new List<Order>();

            // Text search on RowKey (Order ID).
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                all = all.Where(o =>
                    !string.IsNullOrEmpty(o.RowKey) &&
                    o.RowKey.Contains(term, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Status filter (exact, case-insensitive).
            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                all = all.Where(o => string.Equals(o.Status, s, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Date range filters (inclusive end).
            if (dateFrom.HasValue)
                all = all.Where(o => o.OrderDate >= dateFrom.Value).ToList();

            if (dateTo.HasValue)
            {
                var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                all = all.Where(o => o.OrderDate <= end).ToList();
            }

            // Total price range.
            if (minTotal.HasValue)
                all = all.Where(o => o.TotalPrice >= minTotal.Value).ToList();

            if (maxTotal.HasValue)
                all = all.Where(o => o.TotalPrice <= maxTotal.Value).ToList();

            // Item count range.
            if (minItems.HasValue)
                all = all.Where(o => o.ItemCount >= minItems.Value).ToList();

            if (maxItems.HasValue)
                all = all.Where(o => o.ItemCount <= maxItems.Value).ToList();

            // Default sort: newest first.
            all = all.OrderByDescending(o => o.OrderDate).ToList();

            // Prepare customer name map and statuses for the view to render.
            var names = await BuildCustomerNamesAsync(all.Select(o => o.CustomerId));
            ViewData["CustomerNames"] = names;
            ViewData["Statuses"] = GetAllStatuses();

            // Echo filters back to the view for form fields.
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

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Admin: Detailed view for a single order; provides name dictionary and status list for UI.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminDetails(string id, string returnUrl = null)
        {
            // Guard: id is required.
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            // Load order entity from Table Storage.
            var entity = await _order.GetOrderByIdAsync(id);
            if (entity == null) return NotFound();

            // Provide customer name map and statuses for the view.
            ViewData["CustomerNames"] = await BuildCustomerNamesAsync(new[] { entity.CustomerId });
            ViewData["Statuses"] = GetAllStatuses();

            // Preserve or compute a safe returnUrl for the back link.
            var fallback = Url.Action(nameof(AdminDetails), new { id });
            ViewData["returnUrl"] =
                !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : fallback;

            return View(entity);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Admin: Update an order's status and return to the originating page when possible.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, string status, string returnUrl = null)
        {
            // Guard: required parameters.
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(status))
                return BadRequest();

            // Fetch order; 404 if missing.
            var entity = await _order.GetOrderByIdAsync(id);
            if (entity == null)
                return NotFound();

            // Apply new status and persist.
            entity.Status = status.Trim();
            await _order.UpdateOrderAsync(entity);

            // If a safe returnUrl was supplied, use it; otherwise stay on AdminDetails.
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("AdminDetails", new { id });
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Customer: Self-filtered "My Orders" list with basic search and ranges.
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> MyOrders(string q = null, string status = null, DateTime? dateFrom = null, DateTime? dateTo = null, double? minTotal = null,
            double? maxTotal = null, int? minItems = null, int? maxItems = null)
        {
            var uid = CurrentUserId();

            // Load all orders then filter down to the current user's.
            var all = await _order.GetAllOrdersAsync() ?? new List<Order>();
            var mine = all.Where(o => o.CustomerId == uid).ToList();

            // Text search on Order ID to mirror placeholder.
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                mine = mine.Where(o =>
                    !string.IsNullOrEmpty(o.RowKey) &&
                    o.RowKey.Contains(term, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Status filter.
            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                mine = mine.Where(o => string.Equals(o.Status, s, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Date range (inclusive end).
            if (dateFrom.HasValue)
                mine = mine.Where(o => o.OrderDate >= dateFrom.Value).ToList();

            if (dateTo.HasValue)
            {
                var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                mine = mine.Where(o => o.OrderDate <= end).ToList();
            }

            // Total ranges.
            if (minTotal.HasValue)
                mine = mine.Where(o => o.TotalPrice >= minTotal.Value).ToList();
            if (maxTotal.HasValue)
                mine = mine.Where(o => o.TotalPrice <= maxTotal.Value).ToList();

            // Item count ranges.
            if (minItems.HasValue)
                mine = mine.Where(o => o.ItemCount >= minItems.Value).ToList();
            if (maxItems.HasValue)
                mine = mine.Where(o => o.ItemCount <= maxItems.Value).ToList();

            // Default sort: newest to oldest.
            mine = mine.OrderByDescending(o => o.OrderDate).ToList();

            // Provide name map and statuses for the view.
            ViewData["CustomerNames"] = await BuildCustomerNamesAsync(mine.Select(o => o.CustomerId));
            ViewData["Statuses"] = GetAllStatuses();

            // Echo filters back for the UI.
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

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Customer: Single-order details with authorization check and safe return URL.
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> Details(string id, string returnUrl = null)
        {
            // Guard: id must be supplied.
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            // Fetch order by id; 404 if missing.
            var entity = await _order.GetOrderByIdAsync(id);
            if (entity == null)
                return NotFound();

            // Ensure the current user owns this order; otherwise forbid.
            var uid = CurrentUserId();
            if (!string.Equals(entity.CustomerId, uid, StringComparison.Ordinal))
                return Forbid();

            // Provide name map (for a single id) to the view.
            ViewData["CustomerNames"] = await BuildCustomerNamesAsync(new[] { entity.CustomerId });

            // Set title and compute a safe return target.
            ViewData["Title"] = $"Order {entity.RowKey}";
            ViewData["returnUrl"] = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action("MyOrders");

            return View("Details", entity);
        }

    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
