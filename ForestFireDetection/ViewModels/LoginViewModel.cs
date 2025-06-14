﻿using System.ComponentModel.DataAnnotations;

namespace ForestFireDetection.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email adress is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

    }
}
