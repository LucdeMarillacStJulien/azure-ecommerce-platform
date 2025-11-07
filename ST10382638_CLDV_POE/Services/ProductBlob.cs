// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using static System.Net.WebRequestMethods;

namespace ST10382638_CLDV_POE.Services
{
    /// <summary>
    /// Handles product media storage in Azure Blob Storage.
    /// Supports upload (with optional content type) and deletion.
    /// </summary>
    public class ProductBlob
    {
        private readonly BlobContainerClient _containerClient;
        private readonly string _containerName = "productimages";
        private readonly IConfiguration _config;

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes the blob container client and ensures the container exists.
        /// </summary>
        /// <param name="config">Configuration containing AzureStorage:ConnectionString.</param>
        public ProductBlob(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            _containerClient = new BlobContainerClient(connectionString, _containerName);
            _containerClient.CreateIfNotExists(PublicAccessType.Blob);
            _config = config;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Uploads a stream to Blob Storage with the given file name.
        /// Optionally sets the HTTP Content-Type header.
        /// </summary>
        /// <param name="fileStream">The stream to upload.</param>
        /// <param name="fileName">Destination blob name (including extension).</param>
        /// <param name="contentType">Optional MIME type (e.g., "image/png").</param>
        /// <returns>Public URL of the uploaded blob.</returns>
        public async Task<string> UploadBlobAsync(Stream fileStream, string fileName, string? contentType = null)
        {
            var _http = new HttpClient();
            
            var baseUrl = _config["Functions:BlobWrite"];
            var url = $"{baseUrl}&fileName={fileName}&container={_containerName}";

            Console.WriteLine($"Uploading to URL: {url}");
            

            using var content = new StreamContent(fileStream);
            if (!string.IsNullOrEmpty(contentType))
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var res = await _http.PostAsync(url, content);
            var urlText = await res.Content.ReadAsStringAsync();
            Console.WriteLine(urlText);
            return urlText;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes a blob if it exists.
        /// </summary>
        /// <param name="fileName">Blob name to delete.</param>
        public async Task DeleteFileAsync(string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
