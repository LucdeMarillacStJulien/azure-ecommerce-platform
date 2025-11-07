// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ST10382638_CLDV_POE.Models;
using ST10382638_CLDV_POE.Services;

namespace ST10382638_CLDV_POE.Controllers
{
    /// <summary>
    /// Handles Product CRUD with image/video upload to Blob Storage
    /// and corresponding Azure Table Storage persistence.
    /// </summary>
    public class ProductController : Controller
    {
        private readonly ProductTableService _tableStorage;
        private readonly ProductBlob _productBlob;
        private readonly QueueStorageService _queueStorage;

        //------------------------------------------------------------------------------------------------------------------------//
        public ProductController(ProductTableService tableStorage, ProductBlob blob, QueueStorageService queueStorage)
        {
            _tableStorage = tableStorage;
            _productBlob = blob;
            _queueStorage = queueStorage;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // GET: ProductController
        [HttpGet]
        public async Task<IActionResult> Index(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                var all = await _tableStorage.GetAllProductsAsync();
                return View(all);
            }

            try
            {
                var c = await _tableStorage.GetProductByIdAsync(id);
                return View(new List<Product> { c });
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                ViewBag.Message = $"Product with ID '{id}' does not exist.";
                var all = await _tableStorage.GetAllProductsAsync();
                return View(all); // still show all, but message shows up
            }
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // GET: ProductController/Create
        public ActionResult Create()
        {
            return View();
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // POST: ProductController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            ModelState.Remove("imageFile");
            ModelState.Remove("RowKey");
            ModelState.Remove("ImageUrl");

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "Please upload an image or video.");
            }
            else
            {
                if (!HasAllowedExt(imageFile.FileName) || !IsImageOrVideo(imageFile.ContentType))
                    ModelState.AddModelError("imageFile", "Only images or videos are allowed.");
            }

            product.IsAvailable = product.StockQuantity > 0;

            if (ModelState.IsValid)
            {
                try
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    using var stream = imageFile.OpenReadStream();
                    product.ImageUrl = await _productBlob.UploadBlobAsync(stream, fileName, imageFile.ContentType);

                    Console.WriteLine(product.ImageUrl);

                    product.PartitionKey = "Product";
                    product.RowKey = _tableStorage.GetNextRowKey();

                    await _tableStorage.InsertProductAsync(product);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Upload Error: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while uploading the image");
                }
            }

            foreach (var modelState in ModelState)
            {
                foreach (var error in modelState.Value.Errors)
                {
                    Console.WriteLine($"Error in {modelState.Key}: {error.ErrorMessage}");
                }
            }

            return View(product);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        public async Task<IActionResult> Details(string id)
        {
            var product = await _tableStorage.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _tableStorage.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string rowKey, Product product, IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
                return BadRequest();

            // Load current entity FIRST so we can preserve keys, ETag and current image
            var existing = await _tableStorage.GetProductByIdAsync(rowKey);
            if (existing == null)
                return NotFound();

            product.IsAvailable = product.StockQuantity > 0;

            // Preserve keys + ETag for optimistic concurrency
            product.PartitionKey = existing.PartitionKey;
            product.RowKey = existing.RowKey;
            product.ETag = existing.ETag;

            // Validate image ONLY if a new one is provided
            if (imageFile != null && imageFile.Length > 0)
            {
                if (!HasAllowedExt(imageFile.FileName) || !IsImageOrVideo(imageFile.ContentType))
                    ModelState.AddModelError(nameof(imageFile), "Only images or videos are allowed.");
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                product.ImageUrl = existing.ImageUrl;
            }

            if (!TryValidateModel(product))
            {
                // Print errors for debugging
                foreach (var s in ModelState)
                    foreach (var e in s.Value.Errors)
                        Console.WriteLine($"Key: {s.Key} | Error: {e.ErrorMessage}");
                return View(product);
            }

            // If new file provided, replace blob + URL
            if (imageFile != null && imageFile.Length > 0)
            {
                // delete previous blob if you want to avoid orphaned files
                if (!string.IsNullOrWhiteSpace(existing.ImageUrl))
                {
                    var oldFileName = Path.GetFileName(new Uri(existing.ImageUrl).LocalPath);
                    await _productBlob.DeleteFileAsync(oldFileName);
                }

                var newName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                using var stream = imageFile.OpenReadStream();
                product.ImageUrl = await _productBlob.UploadBlobAsync(stream, newName, imageFile.ContentType);
            }

            if (ModelState.IsValid)
            {
                await _tableStorage.UpdateProductAsync(product);
            }

            return RedirectToAction(nameof(Index));
        }

        //------------------------------------------------------------------------------------------------------------------------//
        public async Task<IActionResult> Delete(string id)
        {
            var customer = await _tableStorage.GetProductByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var product = await _tableStorage.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                try
                {
                    var fileName = Path.GetFileName(new Uri(product.ImageUrl).LocalPath);
                    await _productBlob.DeleteFileAsync(fileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Blob delete failed: {ex.Message}");
                }
            }

            await _tableStorage.DeleteProductAsync(id);

            return RedirectToAction(nameof(Index));
        }

        //------------------------------------------------------------------------------------------------------------------------//
        private static bool IsImageOrVideo(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return false;
            contentType = contentType.ToLowerInvariant();
            // Handle common video/image MIME types (some browsers use vendor types)
            return contentType.StartsWith("image/") || contentType.StartsWith("video/");
        }

        //------------------------------------------------------------------------------------------------------------------------//
        private static bool HasAllowedExt(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) return false;

            // Unified, consistent allow-list (with dots), incl. common image & video
            // Add/remove to taste.
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Images
                ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".bmp", ".tif", ".tiff",
                // Videos
                ".mp4", ".m4v", ".webm", ".mov", ".avi", ".mkv", ".ogv", ".3gp", ".mpg", ".mpeg"
            };

            return allowed.Contains(ext);
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//