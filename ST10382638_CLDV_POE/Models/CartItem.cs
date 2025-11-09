using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10382638_CLDV_POE.Models
{
    public class CartItem
    {
        [Required] 
        public string UserId { get; set; } = default!;     // from auth system
        
        [Required] 
        public string ProductId { get; set; } = default!;  // your Product.RowKey or Id
        
        [Range(1, int.MaxValue)] 
        public int Quantity { get; set; } = 1;

        // Denormalized product snapshot so we don't need another model:
        [Required] 
        public string ProductName { get; set; } = default!;
        
        [Required] 
        public decimal UnitPrice { get; set; }             // store price at add time
        
        public string? ImageUrl { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}
