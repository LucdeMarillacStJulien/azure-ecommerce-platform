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
    /// Product entity stored in Azure Table Storage.
    /// Tracks product details, stock, pricing, and availability.
    /// </summary>
    public class Product : ITableEntity
    {
        /// <summary>
        /// Logical partition key. Defaults to "Product".
        /// </summary>
        public string PartitionKey { get; set; } = "Product";

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
        /// Product name.
        /// </summary>
        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        /// <summary>
        /// Product description (max 500 characters).
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Price of the product (must be a positive number).
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        public double Price { get; set; }

        /// <summary>
        /// Product category (e.g., Electronics, Clothing, etc.).
        /// </summary>
        [Required]
        public string? Category { get; set; }

        /// <summary>
        /// Number of units in stock (must be non-negative).
        /// </summary>
        [Required]
        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be a non-negative integer")]
        public int StockQuantity { get; set; }

        /// <summary>
        /// URL of the product image stored in Blob Storage.
        /// </summary>
        [Required]
        [Display(Name = "Image")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Whether the product is available for purchase.
        /// </summary>
        [Required]
        [Display(Name = "Available?")]
        public bool IsAvailable { get; set; }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//