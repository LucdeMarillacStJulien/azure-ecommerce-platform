// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using ST10382638_CLDV_POE.Models;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace ST10382638_CLDV_POE.Services
{
    /// <summary>
    /// Wrapper for Azure Queue Storage used by the Inventory feature.
    /// Provides send and non-destructive peek operations.
    /// </summary>
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;
        private readonly string _queueName = "inventory";
        private readonly IConfiguration _config;

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes the queue client and ensures the queue exists.
        /// </summary>
        /// <param name="config">Configuration providing AzureStorage:ConnectionString.</param>
        public QueueStorageService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            _queueClient = new QueueClient(connectionString, _queueName, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });
            _queueClient.CreateIfNotExists();

            _config = config;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Sends a message to the queue if the message is not null/empty.
        /// </summary>
        /// <param name="message">Plain text message to enqueue.</param>
        public async Task SendMessageAsync(Order order)
        {
            var _http = new HttpClient();
            var baseUrl = _config["Functions:QueueFunction"];
            var url = $"{baseUrl}&mode=Write&queueName={_queueName}";

            var opts = new JsonSerializerOptions { PropertyNamingPolicy = null }; // keep PascalCase
            var json = JsonSerializer.Serialize(order, opts);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await _http.PostAsync(url, content);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Peeks a single message without removing it from the queue.
        /// </summary>
        /// <returns>Message text, or null if no message was available.</returns>
        public async Task<string> PeekMessageAsync()
        {
            var peeked = await _queueClient.PeekMessageAsync();
            return peeked.Value?.MessageText;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Peeks up to 32 messages (non-destructive) and returns their texts.
        /// </summary>
        /// <remarks>
        /// Uses <c>PeekMessagesAsync</c>; does NOT dequeue (no visibility timeout set).
        /// </remarks>
        /// <returns>List of message texts (possibly empty).</returns>
        public async Task<List<String>> PeekAllMessagesAsync()
        {
            var _http = new HttpClient();
            var baseUrl = _config["Functions:QueueFunction"]; 

            var url = $"{baseUrl}&mode=Read&queueName={_queueName}&take=32";

            var json = await _http.GetStringAsync(url); 
            var list = new List<string>();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                foreach (var el in doc.RootElement.EnumerateArray())
                    list.Add(el.GetRawText()); // each element as a JSON string

            return list;
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
