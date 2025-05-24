using Guider.API.MVP.Utility;

namespace Guider.API.MVP.Models.Dto
{
    public class RegisterRequestDTO
    {

        //public string UserName { get; set; } = string.Empty;
        //public string Email { get; set; } = string.Empty;
        //public string Password { get; set; } = string.Empty;
        //public string Role { get; set; } = SD.Role_User;

        // Добавляем свойства для поддержки формата React Admin
        // Эти свойства будут использоваться, если оригинальные свойства не заполнены
        public string username { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        //public string role { get; set; } = string.Empty;

    }
}
