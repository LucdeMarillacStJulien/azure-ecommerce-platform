// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1
//
// References:
// ChatGPT
// https://www.w3schools.com/cs/index.php

using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;

namespace FunctionApp;

/// <summary>
/// Azure Function that can either write to or read from an Azure Storage Queue.
/// Triggered by HTTP GET/POST with query parameters specifying mode and queue name.
/// </summary>
public class QueueFunction
{
    private readonly ILogger<QueueFunction> _logger;
    private readonly IConfiguration _config;
    private QueueClient _queueClient;

    /// <summary>
    /// Constructor for dependency injection of logger and configuration.
    /// </summary>
    public QueueFunction(ILogger<QueueFunction> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// HTTP-triggered function. Supports two modes:
    /// "Write" mode: accepts a message in the request body and enqueues it.
    /// "Read" mode: peeks up to 32 messages from the queue and returns them as JSON.
    /// </summary>
    [Function("QueueFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        var query = HttpUtility.ParseQueryString(req.Url.Query);
        var mode = query["mode"];       // Expected values: "Write" or "Read".
        var queueName = query["queueName"]; // Name of the target queue.

        var conn = _config["AzureWebJobsStorage"]; // Default storage connection string.
        if (conn is null)
        {
            await response.WriteStringAsync($"Connection is empty");
            return response;
        }

        if (mode is null)
        {
            await response.WriteStringAsync($"No mode provided");
            return response;
        }

        if (queueName is null)
        {
            await response.WriteStringAsync($"No queue name provided");
            return response;
        }

        _queueClient = new QueueClient(conn, queueName, new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });

        if (mode.Equals("Write"))
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            await _queueClient.SendMessageAsync(body); // Writes message to the queue.
            await response.WriteStringAsync($"Message sent to queue {queueName}");
        }
        else if (mode.Equals("Read"))
        {
            // Optional ?take=NN (1..32). Default is 32.
            var takeParam = query["take"];
            int take = 32;
            if (!string.IsNullOrEmpty(takeParam) && int.TryParse(takeParam, out var t))
                take = Math.Clamp(t, 1, 32);

            // Peek messages without removing them from the queue.
            var peek = await _queueClient.PeekMessagesAsync(take);

            var items = new List<string>();
            foreach (var m in peek.Value)
                if (!string.IsNullOrWhiteSpace(m.MessageText))
                    items.Add(m.MessageText);

            response.Headers.Add("Content-Type", "application/json");
            var json = "[" + string.Join(",", items) + "]"; // Formats messages as JSON array.
            await response.WriteStringAsync(json);
        }

        return response;
    }

    /// <summary>
    /// Utility method to send a message synchronously if provided string is not null or empty.
    /// </summary>
    private static void SendMessage(QueueClient queueClient, string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            queueClient.SendMessage(message);
        }
    }
}
