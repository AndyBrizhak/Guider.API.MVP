using Guider.API.MVP.Data;
using Guider.API.MVP.Models;
using Guider.API.MVP.Models.Dto;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

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




        [HttpPost("login")]
        
        public async Task<ActionResult<ApiResponse>> Login([FromBody] LoginRequestDTO model)
        {
            ApplicationUser userFromDb = _db.ApplicationUsers
                .FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(userFromDb, model.Password);

            if (userFromDb == null || !isValid)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status401Unauthorized;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Username or password is incorrect!" };
                return BadRequest(_response);
            }

            // Generate JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userFromDb.Id.ToString()),
                   new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, userFromDb.UserName),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, userFromDb.Email),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, SD.Role_User)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);


            LoginResponseDTO loginResponse = new()
            {
                UserId = userFromDb.Id,
                UserName = userFromDb.UserName,
                Email = userFromDb.Email,
                //Token = "test"
                Token = tokenString
                //Roles = userRoles
            };

            if (loginResponse == null)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status404NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Error while login");
                return NotFound(_response);
            }

            _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status200OK;
            _response.IsSuccess = true;
            _response.Result = loginResponse;
            return Ok(_response);

        }

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="model">The registration request model containing user details.</param>
        /// <returns>An ApiResponse indicating the result of the registration process.</returns>
        ///     
        /// <returns></returns>
        
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterRequestDTO model)
        {
            if (model.Password.Length < 6)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status400BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Password must be at least 6 characters long!" };
                return BadRequest(_response);
            }

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
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            try
            {
                var result = await _userManager.CreateAsync(newUser, model.Password);
                if (result.Succeeded)
                {
                    if (!_roleManager.RoleExistsAsync(SD.Role_Super_Admin).GetAwaiter().GetResult())
                    {
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Super_Admin));
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Manager));
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_User));
                    }

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
