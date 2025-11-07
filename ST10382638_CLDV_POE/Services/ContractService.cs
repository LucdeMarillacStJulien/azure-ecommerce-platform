// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

using Azure.Storage.Files.Shares;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;

namespace ST10382638_CLDV_POE.Services
{
    /// <summary>
    /// Service class for managing contract files in Azure File Share storage.
    /// Provides methods to upload, list, download, and delete files.
    /// </summary>
    public class ContractService
    {
        private readonly ShareClient _shareClient;
        private readonly string _shareName = "contracts";
        private readonly IConfiguration _config;

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes the ContractService with the given configuration.
        /// Creates the File Share if it does not already exist.
        /// </summary>
        /// <param name="config">App configuration to retrieve the Azure Storage connection string.</param>
        public ContractService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            _shareClient = new ShareClient(connectionString, _shareName);
            _shareClient.CreateIfNotExists();
            _config = config;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Uploads a file to the root directory of the contracts File Share.
        /// </summary>
        /// <param name="file">The file to be uploaded.</param>
        public async Task UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return;

            var baseUrl = _config["Functions:FileWrite"];
            var url = $"{baseUrl}&fileName={file.FileName}&shareName={_shareName}&length={file.Length}";
            Console.WriteLine(url);

            using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            using var client = new HttpClient();
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves a list of all files stored in the contracts File Share.
        /// </summary>
        /// <returns>A list of file names.</returns>
        public async Task<List<string>> ListFilesAsync()
        {
            var directory = _shareClient.GetRootDirectoryClient();
            var files = new List<string>();

            await foreach (var fileItem in directory.GetFilesAndDirectoriesAsync())
            {
                if (!fileItem.IsDirectory)
                {
                    files.Add(fileItem.Name);
                }
            }

            return files;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Downloads a specified file from the contracts File Share.
        /// </summary>
        /// <param name="fileName">The name of the file to download.</param>
        /// <returns>A stream containing the file contents.</returns>
        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            var directory = _shareClient.GetRootDirectoryClient();
            var fileClient = directory.GetFileClient(fileName);
            var download = await fileClient.DownloadAsync();
            return download.Value.Content;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes a specified file from the contracts File Share if it exists.
        /// </summary>
        /// <param name="fileName">The name of the file to delete.</param>
        /// <returns>True if the file was deleted, false if it did not exist.</returns>
        public async Task<bool> DeleteFileAsync(string fileName)
        {
            var file = _shareClient.GetRootDirectoryClient().GetFileClient(fileName);
            var resp = await file.DeleteIfExistsAsync();
            return resp.Value;
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//