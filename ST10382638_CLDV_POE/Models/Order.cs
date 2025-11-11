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
    /// Order entity stored in Azure Table Storage.
    /// Tracks product purchases, quantities, pricing, and status.
    /// </summary>
    public class Order : ITableEntity
    {
        /// <summary>
        /// Logical partition key. Defaults to "Order".
        /// </summary>
        public string PartitionKey { get; set; } = "Order";

        /// <summary>
        /// Unique row identifier (acts as primary key).
        /// </summary>
        [Key]
        public string RowKey { get; set; }

        /// <summary>
        /// Server-maintained timestamp for this entity.
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Concurrency token used for optimistic concurrency control.
        /// </summary>
        public ETag ETag { get; set; }

        /// <summary>
        /// The ID of the customer who placed the order.
        /// </summary>
        [Required]
        [Display(Name = "Customer ID")]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// Total cost of the order (UnitPrice × Quantity).
        /// </summary>
        [Range(0, 10_000_000)]
        public double TotalPrice { get; set; }

        /// <summary>
        /// Date and time the order was placed (default = UTC now).
        /// </summary>
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Current status of the order (Placed, Processing, Completed, Cancelled).
        /// </summary>
        [Required]
        [StringLength(32)]
        public string Status { get; set; } = "Placed";

        [Required]
        public string ItemsJson { get; set; } = "[]";

        [Range(0, 1_000_000)]
        public int ItemCount { get; set; } 
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//