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
using static System.Net.WebRequestMethods;

namespace ST10382638_CLDV_POE.Services
{
    /// <summary>
    /// Azure Table Storage service for Customer entities.
    /// Handles CRUD operations and simple sequential RowKey generation.
    /// </summary>
    public class CustomerTableService
    {
        private readonly TableClient _tableClient;
        private readonly string _tableName = "Customer";
        
        IConfiguration _config;

        

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Inserts a new customer entity.
        /// </summary>
        /// <param name="customer">Customer to add.</param>
        public async Task InsertCustomerAsync(Customer customer)
        {
            var _http = new HttpClient();
            var baseUrl = _config["Functions:TableWrite"];

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var json = JsonSerializer.Serialize(customer, options);
            var content = new StringContent(json, Encoding.UTF8, "appllication/json");

            var url = $"{baseUrl}&tableName={_tableName}";
            var response = await _http.PostAsJsonAsync(url, customer);
            Console.WriteLine(response);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all customers in the table.
        /// </summary>
        /// <remarks>
        /// Method name retained for compatibility (<c>GetAllStudentAsync</c>) though it returns customers.
        /// </remarks>
        /// <returns>List of all customers.</returns>
        //public async Task<List<Customer>> GetAllCustomersAsync()
        //{
        //    var customers = new List<Customer>();
        //    await foreach (var entity in _tableClient.QueryAsync<Customer>())
        //    {
        //        customers.Add(entity);
        //    }
        //    return customers;
        //}

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves a single customer by RowKey from partition "Customer".
        /// </summary>
        /// <param name="rowKey">Customer RowKey.</param>
        //public async Task<Customer> GetCustomerByIdAsync(string rowKey)
        //{
        //    return await _tableClient.GetEntityAsync<Customer>("Customer", rowKey);
        //}

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Replaces an existing customer using optimistic concurrency via the entity ETag.
        /// </summary>
        /// <param name="customer">Customer with updated fields and a valid ETag.</param>
        //public async Task UpdateCustomerAsync(Customer customer)
        //{
        //    await _tableClient.UpdateEntityAsync(customer, customer.ETag, TableUpdateMode.Replace);
        //}

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes a customer by RowKey from partition "Customer".
        /// </summary>
        /// <param name="rowKey">Customer RowKey.</param>
        //public async Task DeleteCustomerAsync(string rowKey)
        //{
        //    await _tableClient.DeleteEntityAsync("Customer", rowKey);
        //}

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Generates the next numeric RowKey by scanning existing keys in the "Customer" partition.
        /// </summary>
        /// <remarks>
        /// Simple max+1 strategy; suitable for demos/small datasets.  
        /// For higher concurrency/scale, consider GUIDs or a dedicated counter.
        /// </remarks>
        /// <returns>Next RowKey as a string.</returns>
        //public string GetNextRowKey()
        //{
        //    Pageable<Customer> customers = _tableClient.Query<Customer>(c => c.PartitionKey == "Customer");

        //    int max = 0;
        //    foreach (var customer in customers)
        //    {
        //        if (int.TryParse(customer.RowKey, out int rowKey))
        //        {
        //            if (rowKey > max)
        //            {
        //                max = rowKey;
        //            }
        //        }
        //    }

        //    return (max + 1).ToString();
        //}
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//