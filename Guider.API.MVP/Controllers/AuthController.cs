using Guider.API.MVP.Data;
using Guider.API.MVP.Models;
using Guider.API.MVP.Models.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Guider.API.MVP.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ApiResponse _response;
        private readonly string secretKey;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthController(ApplicationDbContext db,
                                IConfiguration configuration,
                                RoleManager<IdentityRole> roleManager,
                                UserManager<ApplicationUser> userManager)
        {
            _db = db;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret") ?? 
                throw new ArgumentNullException(nameof(configuration), "Secret key cannot be null");
            _response = new ApiResponse();
            _roleManager = roleManager;
            _userManager = userManager;
        }

        //[HttpPost("register")]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterRequestDTO model)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            var user = new ApplicationUser()
        //            {
        //                UserName = model.UserName,
        //                Email = model.Email,
        //                PhoneNumber = model.PhoneNumber,
        //                Id = Guid.NewGuid().ToString(),
        //            };
        //            var result = await _userManager.CreateAsync(user, model.Password);
        //            if (result.Succeeded)
        //            {
        //                _response.Result = user;
        //                return Ok(_response);
        //            }
        //            else
        //            {
        //                foreach (var error in result.Errors)
        //                {
        //                    ModelState.AddModelError("Error", error.Description);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add(ex.ToString());
        //    }
        //    return BadRequest(ModelState);
        //}
    }
}
