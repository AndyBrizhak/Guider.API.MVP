namespace Guider.API.MVP.Models.Dto
{
    public class UpdateUserDTO
    {
        //public string? UserName { get; set; }
        //public string? Email { get; set; }
        //public string? Role { get; set; }

        // React Admin style properties (lowercase first letter)
        public string username { get; set; }
        public string email { get; set; }
        public string role { get; set; }
    }
}
