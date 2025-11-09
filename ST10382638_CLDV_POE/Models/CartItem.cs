using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10382638_CLDV_POE.Models
{
    public class CartItem
    {
        [Required, MaxLength(128)]
        public string UserId { get; set; } = default!;

        [Required, MaxLength(64)]
        public string ProductId { get; set; } = default!;   // maps to Product.RowKey

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
