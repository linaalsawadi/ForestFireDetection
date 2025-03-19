using ForestFireDetection.Data;
using System.ComponentModel.DataAnnotations;

namespace ForestFireDetection.ViewModels
{
    public class RegisterViewModel
    {
        [Display(Name = "Email address")]
        [Required(ErrorMessage = "Email address is required")]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "FirstName is required")]
        [Display(Name = "FirstName")]
        public string FirstName { get; set; }

		[Required(ErrorMessage = "LastName is required")]
		[Display(Name = "LastName")]
		public string LastName { get; set; }




		[Required]
        public string Password { get; set; }
        [Display(Name = "Confirm password")]
        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password do not match")]
        public string ConfirmPassword { get; set; }
    }
}
