// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10382638_CLDV_POE.Models
{
    /// <summary>
    /// Represents a system user with authentication credentials and role associations.
    /// Used by both Admin and Customer accounts.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Primary key for the User entity (auto-generated).
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        /// <summary>
        /// User's email address (used for login).
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// User's plaintext password (should be hashed in production).
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// Navigation property linking this user to multiple roles.
        /// </summary>
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
