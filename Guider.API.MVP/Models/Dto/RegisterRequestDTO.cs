using Guider.API.MVP.Utility;

namespace Guider.API.MVP.Models.Dto
{
    public class RegisterRequestDTO
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        //public string Role { get; set; } = SD.Role_User;
  
    }
}
