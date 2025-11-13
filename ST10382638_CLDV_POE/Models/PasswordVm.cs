// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

using System.ComponentModel.DataAnnotations;

namespace ST10382638_CLDV_POE.Models
{
    /// <summary>
    /// ViewModel used for password creation and confirmation during registration.
    /// </summary>
    public class PasswordVm
    {
        /// <summary>
        /// Primary password field for user input.
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// Confirmation field to verify password match.
        /// </summary>
        [Required]
        public string ConfirmPassword { get; set; }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
