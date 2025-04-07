using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Guider.API.MVP.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public override required string Id { get; set; }

        [Required]
        public override required string UserName { get; set; }

        [Required]
        public override required string PasswordHash { get; set; }
    }
}

