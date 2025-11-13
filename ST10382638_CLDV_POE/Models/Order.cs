// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ST10382638_CLDV_POE.Models
{
    /// <summary>
    /// Represents an Order entity stored in Azure Table Storage.
    /// Contains customer, pricing, item, and order status information.
    /// </summary>
    public class Order : ITableEntity
    {
        /// <summary>
        /// Partition key for logical grouping of all Order entities.
        /// Defaults to "Order".
        /// </summary>
        public string PartitionKey { get; set; } = "Order";

        /// <summary>
        /// Unique RowKey (acts as the order identifier/primary key).
        /// </summary>
        [Key]
        public string RowKey { get; set; }

        /// <summary>
        /// Azure-managed timestamp for entity version tracking.
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Azure ETag used for concurrency handling and update control.
        /// </summary>
        public ETag ETag { get; set; }

        /// <summary>
        /// Identifier of the customer who created the order.
        /// Corresponds to the authenticated user’s ID.
        /// </summary>
        [Required]
        [Display(Name = "Customer ID")]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// Total price for the order (sum of UnitPrice × Quantity for all items).
        /// </summary>
        [Range(0, 10_000_000)]
        public double TotalPrice { get; set; }

        /// <summary>
        /// Date and time the order was placed.
        /// Defaults to the current UTC time.
        /// </summary>
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Current order status (Placed, Processing, Completed, or Cancelled).
        /// </summary>
        [Required]
        [StringLength(32)]
        public string Status { get; set; } = "Placed";

        /// <summary>
        /// Serialized JSON representation of the ordered items.
        /// </summary>
        [Required]
        public string ItemsJson { get; set; } = "[]";

        /// <summary>
        /// Total number of individual items within the order.
        /// </summary>
        [Range(0, 1_000_000)]
        public int ItemCount { get; set; }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
