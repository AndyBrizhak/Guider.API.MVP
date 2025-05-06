namespace Guider.API.MVP.Models.Dto
{
    public class LoginRequestDTO
    {
        //public string UserName { get; set; }
        //public string Password { get; set; }

        // Сохраняем существующие свойства для обратной совместимости
        //public string UserName { get; set; }
        //public string Password { get; set; }

        // Добавляем свойства для поддержки формата React Admin
        public string username { get; set; }
        public string password { get; set; }
        public string? Email { get; set; }
    }
}
