namespace Guider.API.MVP.Models.Dto
{
    public class LoginResponseDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string UserName { get; internal set; }
        public string UserId { get; internal set; }
    }
}
