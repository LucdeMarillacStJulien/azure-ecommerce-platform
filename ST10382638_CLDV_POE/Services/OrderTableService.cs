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

namespace ST10382638_CLDV_POE.Services
{
    /// <summary>
    /// Azure Table Storage service for Order entities.
    /// Provides CRUD operations and RowKey generation for orders.
    /// </summary>
    public class OrderTableService
    {
        private readonly TableClient _table;
        private readonly string _tableName = "Orders";

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes the table client and ensures the Orders table exists.
        /// </summary>
        /// <param name="config">Configuration to access connection string.</param>
        /// <param name="productService">Injected ProductTableService (not directly used, but keeps DI aligned).</param>
        public OrderTableService(IConfiguration config, ProductTableService productService)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            _table = new TableClient(connectionString, _tableName);
            _table.CreateIfNotExists();
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Inserts a new order into the table.
        /// </summary>
        /// <param name="order">Order entity to insert.</param>
        public async Task InsertOrderAsync(Order order)
        {
            await _table.AddEntityAsync(order);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all orders stored in the table.
        /// </summary>
        /// <returns>List of all orders.</returns>
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();
            await foreach (var entity in _table.QueryAsync<Order>())
            {
                orders.Add(entity);
            }
            return orders;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves a single order by RowKey from partition "Order".
        /// </summary>
        /// <param name="rowKey">Order RowKey.</param>
        /// <returns>The matching order entity.</returns>
        public async Task<Order> GetOrderByIdAsync(string rowKey)
        {
            return await _table.GetEntityAsync<Order>("Order", rowKey);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Updates an existing order using optimistic concurrency with ETag.
        /// </summary>
        /// <param name="order">Order entity with changes applied.</param>
        public async Task UpdateOrderAsync(Order order)
        {
            await _table.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes an order by RowKey from partition "Order".
        /// </summary>
        /// <param name="rowKey">Order RowKey.</param>
        public async Task DeleteOrderAsync(string rowKey)
        {
            await _table.DeleteEntityAsync("Order", rowKey);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Generates the next numeric RowKey by scanning existing orders.
        /// </summary>
        /// <remarks>
        /// Simple max+1 approach; works for small datasets.  
        /// For concurrency or high scale, consider GUIDs or distributed counters.
        /// </remarks>
        /// <returns>Next RowKey as a string.</returns>
        public string GetNextRowKey()
        {
            Pageable<Order> orders = _table.Query<Order>(o => o.PartitionKey == "Order");
            int max = 0;

            foreach (var order in orders)
            {
                if (int.TryParse(order.RowKey, out int rowKey))
                {
                    if (rowKey > max)
                    {
                        max = rowKey;
                    }
                }
            }
            return (max + 1).ToString();
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
