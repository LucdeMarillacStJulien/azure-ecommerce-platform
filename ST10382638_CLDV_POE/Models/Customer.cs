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
    /// Customer entity stored in Azure Table Storage.
    /// Implements ITableEntity and uses DataAnnotations for MVC validation/display.
    /// </summary>
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        /// <summary>
        /// Customer's given name.
        /// </summary>
        [Required]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; }

        /// <summary>
        /// Customer's family/surname.
        /// </summary>
        [Required]
        [Display(Name = "Surname")]
        [StringLength(50)]
        public string LastName { get; set; }

        /// <summary>
        /// Date of birth (store/normalize as UTC in controller/service).
        /// </summary>
        [Required]
        [Display(Name = "Date of Birth")]
        public DateTime DOB { get; set; }

        /// <summary>
        /// Contact phone number.
        /// </summary>
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Contact email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Company/Organization name.
        /// </summary>
        [Required]
        [StringLength(50)]
        [Display(Name = "Company Name")]
        public string Company { get; set; }

        /// <summary>
        /// Primary street address line.
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
        /// City/Town.
        /// </summary>
        [Required]
        public string City { get; set; }

        /// <summary>
        /// Province/State/Region.
        /// </summary>
        [Required]
        [Display(Name = "Province or State")]
        public string State { get; set; }

        /// <summary>
        /// Postal/ZIP code.
        /// </summary>
        [Required]
        [Display(Name = "Zip Code")]
        public string ZipCode { get; set; }

        /// <summary>
        /// Country name (ISO suggestions optional).
        /// </summary>
        [Required]
        public string Country { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//