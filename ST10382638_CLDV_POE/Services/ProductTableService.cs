// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

using Azure;
using Azure.Data.Tables;
using ST10382638_CLDV_POE.Models;
using System.Text;
using System.Text.Json;

namespace ST10382638_CLDV_POE.Services
{
    /// <summary>
    /// Azure Table Storage service for Product entities.
    /// Provides CRUD operations, simple RowKey generation, and stock reservation helpers.
    /// </summary>
    public class ProductTableService
    {
        private readonly TableClient _tableClient;
        private readonly string _tableName = "Products";
        private readonly IConfiguration _config;

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes the table client and ensures the Products table exists.
        /// </summary>
        /// <param name="config">Configuration providing AzureStorage:ConnectionString.</param>
        public ProductTableService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            _tableClient = new TableClient(connectionString, _tableName);
            _tableClient.CreateIfNotExists();

            _config = config;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Inserts a new product entity.
        /// </summary>
        /// <param name="product">Product to add.</param>
        public async Task InsertProductAsync(Product product)
        {
            var _http = new HttpClient();
            var baseUrl = _config["Functions:TableWrite"];

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var json = JsonSerializer.Serialize(product, options);
            var content = new StringContent(json, Encoding.UTF8, "appllication/json");

            var url = $"{baseUrl}&tableName={_tableName}";
            var response = await _http.PostAsJsonAsync(url, product);
            Console.WriteLine(response);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all products.
        /// </summary>
        /// <returns>List of all products.</returns>
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var entity in _tableClient.QueryAsync<Product>())
            {
                products.Add(entity);
            }
            return products;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves a product by RowKey from partition "Product".
        /// </summary>
        /// <param name="rowKey">Product RowKey.</param>
        /// <returns>The product entity, if found.</returns>
        public async Task<Product> GetProductByIdAsync(string rowKey)
        {
            return await _tableClient.GetEntityAsync<Product>("Product", rowKey);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Replaces an existing product using optimistic concurrency via the entity ETag.
        /// </summary>
        /// <param name="product">Product with updated fields and a valid ETag.</param>
        public async Task UpdateProductAsync(Product product)
        {
            await _tableClient.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes a product by RowKey from partition "Product".
        /// </summary>
        /// <param name="rowKey">Product RowKey.</param>
        public async Task DeleteProductAsync(string rowKey)
        {
            await _tableClient.DeleteEntityAsync("Product", rowKey);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Attempts to reserve (decrease) stock for a product.
        /// Returns false if insufficient stock or product not found.
        /// </summary>
        /// <remarks>
        /// Uses <c>ETag.All</c> to bypass concurrency checks for simplicity.  
        /// Consider using the real ETag for strict optimistic concurrency in high-contention scenarios.
        /// </remarks>
        /// <param name="productId">Product RowKey.</param>
        /// <param name="qty">Quantity to reserve.</param>
        /// <returns>True if reserved; otherwise false.</returns>
        public async Task<bool> TryReserveStockAsync(string productId, int qty)
        {
            if (qty <= 0) return true;

            var e = await GetProductByIdAsync(productId); // PartitionKey = "Product"
            if (e == null) return false;

            if (e.StockQuantity < qty) return false; // block oversell
            e.StockQuantity -= qty;                  // decrease (reserve)

            await _tableClient.UpdateEntityAsync(e, ETag.All, TableUpdateMode.Replace);
            return true;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Releases (increases) stock for a product.
        /// </summary>
        /// <remarks>
        /// Uses <c>ETag.All</c> for simplicity; switch to real ETag if needed for concurrency.
        /// </remarks>
        /// <param name="productId">Product RowKey.</param>
        /// <param name="qty">Quantity to release.</param>
        public async Task ReleaseStockAsync(string productId, int qty)
        {
            if (qty <= 0) return;

            var e = await GetProductByIdAsync(productId);
            if (e == null) return;

            e.StockQuantity += qty; // increase (release)
            await _tableClient.UpdateEntityAsync(e, ETag.All, TableUpdateMode.Replace);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Generates the next numeric RowKey by scanning existing keys in the "Product" partition.
        /// </summary>
        /// <remarks>
        /// Simple max+1 strategy; fine for demos/small datasets.  
        /// For scale/concurrency, prefer GUIDs or a dedicated counter.
        /// </remarks>
        /// <returns>Next RowKey as a string.</returns>
        public string GetNextRowKey()
        {
            Pageable<Product> products = _tableClient.Query<Product>(c => c.PartitionKey == "Product");

            int max = 0;
            foreach (var product in products)
            {
                if (int.TryParse(product.RowKey, out int rowKey))
                {
                    if (rowKey > max)
                    {
                        max = rowKey;
                    }
                }
            }

            return (max + 1).ToString();
        }

        //------------------------------------------------------...--------------------------------------------------------------//
        /// <summary>
        /// Attempts to reserve (decrease) stock for a collection of cart items.
        /// Ensures all items have sufficient stock; if any reservation fails,
        /// previously reserved stock is rolled back.
        /// </summary>
        /// <param name="items">Cart items to reserve stock for.</param>
        /// <returns>
        /// True if stock was successfully reserved for all items; otherwise false.
        /// </returns>
        public async Task<bool> TryReserveStockForItemsAsync(IEnumerable<CartItem> items)
        {
            if (items == null)
                return false;

            // Track successful reservations for rollback if needed
            var reserved = new List<(string ProductId, int Quantity)>();

            foreach (var item in items)
            {
                if (item == null) continue;
                if (string.IsNullOrWhiteSpace(item.ProductId)) continue;

                var qty = item.Quantity;
                if (qty <= 0) continue;

                var ok = await TryReserveStockAsync(item.ProductId, qty);
                if (!ok)
                {
                    // Roll back any stock we already reserved
                    foreach (var r in reserved)
                    {
                        await ReleaseStockAsync(r.ProductId, r.Quantity);
                    }

                    return false;
                }

                reserved.Add((item.ProductId, qty));
            }

            return true;
        }

    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//