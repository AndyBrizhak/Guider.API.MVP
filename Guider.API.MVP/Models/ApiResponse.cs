using System.Net;

namespace Guider.API.MVP.Models
{
    public class ApiResponse
    {
        public ApiResponse()
        {
            //StatusCode = HttpStatusCode.OK;
            //IsSuccess = true;
            ErrorMessages = new List<string>();
        }
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; } = true;
        public List<string>? ErrorMessages { get; set; }
        public object? Result { get; set; }
    }
}
