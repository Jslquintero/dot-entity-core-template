using System.ComponentModel.DataAnnotations;

namespace Template.Api.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [Display(Name = "Email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [Display(Name = "Password")]
        public required string Password { get; set; }

    }
}
