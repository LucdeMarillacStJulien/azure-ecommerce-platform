// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1
//
// References:
// ChatGPT, https://chat.openai.com/
// W3Schools, https://www.w3schools.com/cs/index.php

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;

namespace FunctionApp
{
    /// <summary>
    /// Azure Function to write/upload files into Azure Blob Storage.
    /// Triggered via HTTP POST request to /api/blobs?fileName=...&container=...
    /// </summary>
    public class BlobWriteFunction
    {
        private readonly ILogger<BlobWriteFunction> _logger;
        private readonly IConfiguration _config;

        /// <summary>
        /// Constructor with dependency injection for logger and configuration.
        /// </summary>
        public BlobWriteFunction(ILogger<BlobWriteFunction> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// HTTP-triggered function that uploads a file stream to Azure Blob Storage.
        /// </summary>
        [Function("BlobWrite")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "blobs")] HttpRequestData req)
        {
            // Parse query string for file and container details
            var query = HttpUtility.ParseQueryString(req.Url.Query);
            var fileName = query["fileName"];
            var containerName = query["container"];

            // Connect to blob container
            var conn = _config["AzureStorage:ConnectionString"];
            var container = new BlobContainerClient(conn, containerName);
            var blob = container.GetBlobClient(fileName);

            // Check content type from request header
            var contentType = req.Headers.TryGetValues("Content-Type", out var vals)
                ? vals.FirstOrDefault()
                : null;

            // Upload with headers if content type exists, else upload normally
            if (!string.IsNullOrEmpty(contentType))
            {
                var options = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                };
                await blob.UploadAsync(req.Body, options);
            }
            else
            {
                await blob.UploadAsync(req.Body, overwrite: true);
            }

            // Return blob URI in response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(blob.Uri.ToString());
            return response;
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
