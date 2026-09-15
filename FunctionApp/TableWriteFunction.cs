// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1
//
// References:
// ChatGPT
// https://www.w3schools.com/cs/index.php

using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ST10382638_CLDV_POE.Data;
using ST10382638_CLDV_POE.Models;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using System.Web;

namespace FunctionApp;

/// <summary>
/// Handles writing Customer or Product data to Azure Table Storage using an HTTP-triggered Azure Function.
/// </summary>
public class TableWriteFunction
{
    private readonly ILogger<TableWriteFunction> _logger;
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    public TableWriteFunction(ILogger<TableWriteFunction> logger, IConfiguration config, AppDbContext context)
    {
        _logger = logger;
        _config = config;
        _context = context;
    }

    /// <summary>
    /// Triggered by an HTTP POST request. Expects ?tableName=Customer or Product in the query string
    /// and a JSON body containing the model data.
    /// </summary>
    [Function("TableWrite")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "tables")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        var query = HttpUtility.ParseQueryString(req.Url.Query);
        var tableName = query["tableName"];
        if (tableName is null)
            Console.WriteLine("No table name provided");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        string body = await new StreamReader(req.Body).ReadToEndAsync();

        if (tableName.Equals("Customer"))
        {
            Customer? customer = JsonSerializer.Deserialize<Customer>(body, options);
            _context.Customer.Add(customer);
            await _context.SaveChangesAsync();
            await response.WriteStringAsync($"Customer {customer.FirstName} {customer.LastName} processed.");
        }
        else
        {
            var conn = GetTableClient(tableName);
            Product? product = JsonSerializer.Deserialize<Product>(body, options);
            await conn.AddEntityAsync(product);
            await response.WriteStringAsync($"Product {product.ProductName} processed.");
        }

        return response;
    }

    /// <summary>
    /// Creates or returns an existing Azure Table client for the given table.
    /// </summary>
    private TableClient GetTableClient(string tableName)
    {
        var connectionString = _config["AzureWebJobsStorage"];
        var tableClient = new TableClient(connectionString, tableName);
        tableClient.CreateIfNotExists();
        return tableClient;
    }
}