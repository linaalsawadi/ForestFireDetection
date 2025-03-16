using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ForestFireDetection.Areas.Identity.Data;

public class ApplicationUser: IdentityUser
{
    [PersonalData]
    [Column(TypeName = "nvarchar(100)")]
    public string FirstName { get; set; } = "DefaultFirstName";

    [PersonalData]
    [Column(TypeName = "nvarchar(100)")]
    public string LastName { get; set; } = "DefaultFirstName";
}
