// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ST10382638_CLDV_POE.Services;

namespace ST10382638_CLDV_POE.Controllers
{
    /// <summary>
    /// Handles contract file management (upload, download, delete, and listing) 
    /// using Azure File Share via the ContractService.
    /// </summary>
    public class ContractController : Controller
    {
        private readonly ContractService _fileShareService;

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Constructor to inject the ContractService dependency.
        /// </summary>
        /// <param name="fileShareService">Service for handling Azure File Share operations.</param>
        public ContractController(ContractService fileShareService)
        {
            _fileShareService = fileShareService;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Displays all files currently stored in the File Share.
        /// </summary>
        /// <returns>A view containing a list of files.</returns>
        public async Task<IActionResult> Index()
        {
            var files = await _fileShareService.ListFilesAsync();
            return View(files);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Uploads a file to the File Share.
        /// </summary>
        /// <param name="file">The file selected by the user.</param>
        /// <returns>Redirects back to Index after successful upload.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "No file selected.";
                return RedirectToAction("Index");
            }

            // Allowed extensions
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Invalid file type. Only PDF, DOC, DOCX, and TXT are allowed.";
                return RedirectToAction("Index");
            }

            // ✅ Optional: check MIME type too for extra safety
            var allowedMimeTypes = new[]
            {
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "text/plain"
            };

            if (!allowedMimeTypes.Contains(file.ContentType))
            {
                TempData["Error"] = "Invalid file type based on MIME check.";
                return RedirectToAction("Index");
            }

            // If valid, upload to Azure File Share
            await _fileShareService.UploadFileAsync(file);

            return RedirectToAction("Index");
        }


        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Downloads a specific file from the File Share.
        /// </summary>
        /// <param name="fileName">The name of the file to download.</param>
        /// <returns>File stream as a download response.</returns>
        public async Task<IActionResult> Download(string fileName)
        {
            var stream = await _fileShareService.DownloadFileAsync(fileName);
            return File(stream, "application/octet-stream", fileName);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes a specific file from the File Share.
        /// </summary>
        /// <param name="fileName">The name of the file to delete.</param>
        /// <returns>Returns the view after deletion (can be improved to redirect to Index).</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string fileName)
        {
            var result = await _fileShareService.DeleteFileAsync(fileName);

            // NOTE: Returning View() here might not refresh the list.
            // A redirect to Index would usually make more sense:
            // return RedirectToAction("Index");
            return View();
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
