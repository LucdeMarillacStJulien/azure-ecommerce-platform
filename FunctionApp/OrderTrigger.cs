// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1
//
// References:
// ChatGPT, https://chat.openai.com/
// W3Schools, https://www.w3schools.com/cs/index.php

using Azure.Data.Tables;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ST10382638_CLDV_POE.Models;
using System;
using System.Text.Json;

namespace FunctionApp
{
    /// <summary>
    /// Queue-triggered function that writes Order entities to the Azure Table 'Orders'.
    /// The queue message must contain JSON that matches the Order model.
    /// </summary>
    public class OrderTrigger
    {
        private readonly ILogger<OrderTrigger> _logger;
        private readonly string _storageConnection;
        private TableClient _tableClient;
        private readonly IConfiguration _config;

        /// <summary>
        /// Constructor initializes the TableClient for 'Orders' table.
        /// Uses a storage connection string defined in code.
        /// </summary>
        public OrderTrigger(ILogger<OrderTrigger> logger, IConfiguration config)
        {
            _config = config;
            _logger = logger;
            var serviceClient = new TableServiceClient(_config["AzureWebJobsStorage"]);
            _tableClient = serviceClient.GetTableClient("Orders");
        }

        /// <summary>
        /// Triggered when a new message is added to the 'inventory' queue.
        /// Deserializes the message into an Order object and inserts it into the Orders table.
        /// </summary>
        [Function("OrderQueueTrigger")]
        public async Task OrderWriter([QueueTrigger("inventory", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var order = JsonSerializer.Deserialize<Order>(message.MessageText, options);

            if (order == null)
            {
                _logger.LogError("Failed to deserialize JSON message");
                return;
            }

            _logger.LogInformation($"Saving entity with RowKey: {order.RowKey}");
            await _tableClient.AddEntityAsync(order);
            _logger.LogInformation("Successfully saved order to table");
        }
    }
}
