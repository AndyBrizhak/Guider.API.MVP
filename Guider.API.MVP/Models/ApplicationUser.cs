using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Guider.API.MVP.Models
{
    public class ApplicationUser : IdentityUser
    {
        //[Required]
        //public override string Id { get; set; }

        //[Required]
        public string UserName { get; set; }

        public string Email { get; set; }

        //[Required]
        //public override string? PasswordHash { get; set; }
    }
}

