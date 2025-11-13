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
    /// Represents an Admin user linked to a system User entity.
    /// </summary>
    public class Admin
    {
        // Primary key (auto-incremented identity column).
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AdminId { get; set; }

        // Admin's first name (required, up to 100 chars).
        [Required]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        // Admin's surname (required, up to 100 chars).
        [Required]
        [MaxLength(100)]
        public string Surname { get; set; }

        // Foreign key to associated User record.
        public int UserId { get; set; }

        // Navigation property to the User entity.
        public User User { get; set; }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
