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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ST10382638_CLDV_POE.Services;
using System.Net;

namespace ST10382638_CLDV_POE.Controllers
{
    /// <summary>
    /// Inventory UI endpoints for viewing messages written to the Azure Queue.
    /// Relies on <see cref="QueueStorageService"/> to abstract queue operations.
    /// </summary>
    public class InventoryController : Controller
    {
        private readonly QueueStorageService _queueService;

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// DI constructor for the inventory controller.
        /// </summary>
        /// <param name="queueService">Service wrapper for Azure Queue Storage operations.</param>
        public InventoryController(QueueStorageService queueService)
        {
            _queueService = queueService;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Peeks (non-destructively) all available messages and renders them.
        /// </summary>
        /// <remarks>
        /// This action should return a model that your Peek.cshtml expects (e.g., IEnumerable&lt;QueueMessage&gt; or a custom DTO).
        /// Ensure the view displays the actual message text property exposed by your service (often <c>MessageText</c>).
        /// </remarks>
        /// <returns>Peek view with queue messages.</returns>
        public async Task<IActionResult> Peek()
        {
            var messages = await _queueService.PeekAllMessagesAsync();
            return View(messages);
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
