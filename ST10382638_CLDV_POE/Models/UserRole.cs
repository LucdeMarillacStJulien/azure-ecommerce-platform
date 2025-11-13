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
    /// Represents the many-to-many relationship between Users and Roles.
    /// Each record links one User to one Role.
    /// </summary>
    public class UserRole
    {
        /// <summary>
        /// Primary key for the UserRole entity (auto-generated).
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserRoleId { get; set; }

        /// <summary>
        /// Foreign key linking to the User entity.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Navigation property to the related User.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Foreign key linking to the Role entity.
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// Navigation property to the related Role.
        /// </summary>
        public Role Role { get; set; }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
