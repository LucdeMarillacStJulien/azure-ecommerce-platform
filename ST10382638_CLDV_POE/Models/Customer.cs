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
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10382638_CLDV_POE.Models
{
    /// <summary>
    /// Represents a Customer entity with relational mapping and MVC validation.
    /// Data stored in Azure Table Storage with linkage to the User entity.
    /// </summary>
    public class Customer
    {
        // Primary key generated automatically (EF Identity column).
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        /// <summary>
        /// Customer's first/given name.
        /// </summary>
        [Required]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; }

        /// <summary>
        /// Customer's last/surname.
        /// </summary>
        [Required]
        [Display(Name = "Surname")]
        [StringLength(50)]
        public string LastName { get; set; }

        /// <summary>
        /// Customer's date of birth.
        /// </summary>
        [Required]
        [Display(Name = "Date of Birth")]
        public DateTime DOB { get; set; }

        /// <summary>
        /// Email proxy property linked to the User.Email field.
        /// Not stored directly in database; managed via User navigation property.
        /// </summary>
        [NotMapped]
        [Required]
        [EmailAddress]
        public string Email
        {
            get => User?.Email;
            set
            {
                if (User == null)
                    User = new User();
                User.Email = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Contact phone number for the customer.
        /// </summary>
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Associated company or organization name.
        /// </summary>
        [Required]
        [StringLength(50)]
        [Display(Name = "Company Name")]
        public string Company { get; set; }

        /// <summary>
        /// Primary address line for street or PO box.
        /// </summary>
        [Required]
        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; }

        /// <summary>
        /// Secondary address line (optional).
        /// </summary>
        [Display(Name = "Address Line 2")]
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// City or town name.
        /// </summary>
        [Required]
        public string City { get; set; }

        /// <summary>
        /// Province, state, or regional subdivision.
        /// </summary>
        [Required]
        [Display(Name = "Province or State")]
        public string State { get; set; }

        /// <summary>
        /// Postal or ZIP code for the address.
        /// </summary>
        [Required]
        [Display(Name = "Zip Code")]
        public string ZipCode { get; set; }

        /// <summary>
        /// Country where the customer resides.
        /// </summary>
        [Required]
        public string Country { get; set; }

        // Foreign key linking to User table.
        public int UserId { get; set; }

        // Navigation property representing the linked User record.
        public User User { get; set; }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
