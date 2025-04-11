using Guider.API.MVP.Data;
using Guider.API.MVP.Models;
using Guider.API.MVP.Models.Dto;
using Guider.API.MVP.Utility;
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
        private string secretKey;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        //private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext db,
                                IConfiguration configuration,
                                RoleManager<IdentityRole> roleManager,
                                UserManager<ApplicationUser> userManager)
        {
            //_configuration = configuration;
            _db = db;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret") ?? 
                throw new ArgumentNullException(nameof(configuration), "Secret key cannot be null");
            _response = new ApiResponse();
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpPost("register")]
        
        public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterRequestDTO model)
        {
            ApplicationUser userFromDb = _db.ApplicationUsers
                .FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());
            
           if (userFromDb != null)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status400BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "User already exists!" };
                return BadRequest(_response);
            }

            ApplicationUser newUser = new()
            {

                UserName = model.UserName,
                Email = model.Email,
                NormalizedUserName = model.UserName.ToUpper(),
                NormalizedEmail = model.Email.ToUpper(),
                EmailConfirmed = false, // Значение по умолчанию
                PhoneNumberConfirmed = false, // Значение по умолчанию
                TwoFactorEnabled = false, // Значение по умолчанию
                LockoutEnabled = true, // Значение по умолчанию
                SecurityStamp = Guid.NewGuid().ToString() // Уникальный идентификатор


            };


            try
                {
                    var result = await _userManager.CreateAsync(newUser, model.Password);
                    if (result.Succeeded)
                    {
                        if (!_roleManager.RoleExistsAsync(SD.Role_Super_Admin).GetAwaiter().GetResult())
                        {
                            // create the all roles if it doesn't exist in the database
                            await _roleManager.CreateAsync(new IdentityRole(SD.Role_Super_Admin));
                            await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                            await _roleManager.CreateAsync(new IdentityRole(SD.Role_Manager));
                            await _roleManager.CreateAsync(new IdentityRole(SD.Role_User));
                        }

                        // assign the role to the user only
                        await _userManager.AddToRoleAsync(newUser, SD.Role_User);

                        _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status201Created;

                        _response.IsSuccess = true;
                        _response.Result = newUser;
                        return CreatedAtAction(nameof(Register), new { id = newUser.Id }, _response);
                    }
                }

                    catch (Exception ex)
                    {
                        _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status500InternalServerError;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { ex.Message };
                        return StatusCode(StatusCodes.Status500InternalServerError, _response);
                    }
            
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status400BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Error while registration");
                return BadRequest(_response);
        }
    }
}
