using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ForestFireDetection.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        [Column(TypeName = "nvarchar(100)")]
        public string FirstName { get; set; } = "DefaultFirstName";

        [PersonalData]
        [Column(TypeName = "nvarchar(100)")]
        public string LastName { get; set; } = "DefaultLastName";
    }
}
