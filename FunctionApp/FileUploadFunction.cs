// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1
//
// References:
// ChatGPT, https://chat.openai.com/
// W3Schools, https://www.w3schools.com/cs/index.php

using Azure.Storage.Files.Shares;
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
/// HTTP-triggered Azure Function that uploads a file to Azure File Storage.
/// The request must provide fileName, shareName, and length in the query string.
/// The file content is streamed from the request body.
/// </summary>
public class FileUploadFunction
{
    private readonly ILogger<FileUploadFunction> _logger;
    private readonly IConfiguration _config;

    /// <summary>
    /// Constructor for dependency injection of logger and configuration.
    /// </summary>
    public FileUploadFunction(ILogger<FileUploadFunction> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Handles HTTP POST requests to upload a file to a specified Azure File Share.
    /// Query string parameters: fileName, shareName, and length.
    /// </summary>
    [Function("FileWrite")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "files")] HttpRequestData req)
    {
        var q = HttpUtility.ParseQueryString(req.Url.Query);
        var fileName = q["fileName"];   // Name of the file to create in Azure File Share
        var shareName = q["shareName"]; // Target file share
        long length = long.Parse(q["length"]); // Length of file in bytes

        var shareClient = new ShareClient(_config["AzureWebJobsStorage"], shareName);
        var fileClient = shareClient.GetRootDirectoryClient().GetFileClient(fileName);

        await fileClient.CreateAsync(length);          // Creates the file with specified length
        await fileClient.UploadAsync(req.Body);        // Uploads the content from request body

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteStringAsync(fileClient.Uri.ToString()); // Returns the file URI as confirmation
        return res;
    }
}
