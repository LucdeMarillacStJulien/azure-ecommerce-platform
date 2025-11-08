using System.ComponentModel.DataAnnotations;

namespace ST10382638_CLDV_POE.Models
{
    public class PasswordVm
    {
        [Required]
        public string Password { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }
    }
}
