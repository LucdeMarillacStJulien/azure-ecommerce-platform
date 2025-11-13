// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// https://chatgpt.com/c/690f7551-669c-8329-9b07-041233d40273
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap5/index.php
// https://www.w3schools.com/js/default.asp

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10382638_CLDV_POE.Models
{
    /// <summary>
    /// Represents a shopping cart item linked to a user and product.
    /// </summary>
    public class CartItem
    {
        // Identifier for the user who owns this cart item (from authentication system).
        [Required]
        public string UserId { get; set; } = default!;

        // Identifier for the product (Product.RowKey or Id from Table Storage).
        [Required]
        public string ProductId { get; set; } = default!;

        // Quantity of the product in the cart, must be at least 1.
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        // Snapshot of product name at time of addition (denormalized for performance).
        [Required]
        public string ProductName { get; set; } = default!;

        // Product price at the time of adding to cart.
        [Required]
        public decimal UnitPrice { get; set; }

        // Optional image URL for product preview.
        public string? ImageUrl { get; set; }

        // Timestamp of when item was first created.
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        // Timestamp of last update to item (e.g., quantity change).
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
