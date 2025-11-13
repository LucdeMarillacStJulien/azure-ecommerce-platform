// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10382638_CLDV_POE.Models
{
    /// <summary>
    /// Represents a user role (e.g., Admin or Customer) within the system.
    /// Defines relationship mapping to users via the UserRole join table.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// Primary key for the Role entity (auto-incremented).
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RoleId { get; set; }

        /// <summary>
        /// Name of the role (e.g., "Admin", "Customer").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Navigation property representing all users assigned to this role.
        /// </summary>
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
