using Guider.API.MVP.Data;
using Guider.API.MVP.Models;
using Guider.API.MVP.Models.Dto;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
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



        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="model"></param>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

            // Получить роли пользователя
            var userRoles = await _userManager.GetRolesAsync(userFromDb);
            var userRole = userRoles.FirstOrDefault() ?? "User"; // Если ро

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
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, userRole)
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("users")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse>> GetUsersPaged(int pageNumber = 1, int pageSize = 10)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status401Unauthorized;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Unable to retrieve current user!" };
                return StatusCode(StatusCodes.Status401Unauthorized, _response);
            }

            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            if (currentUserRoles == null || !currentUserRoles.Any())
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status403Forbidden;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Current user has no roles assigned!" };
                return StatusCode(StatusCodes.Status403Forbidden, _response);
            }

            if (!currentUserRoles.Contains(SD.Role_Super_Admin) && !currentUserRoles.Contains(SD.Role_Admin))
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status403Forbidden;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Access denied, with current user role!" };
                return StatusCode(StatusCodes.Status403Forbidden, _response);
            }

            var users = _db.ApplicationUsers
               .Skip((pageNumber - 1) * pageSize)
               .Take(pageSize)
               .ToList();

            if (users == null || !users.Any())
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status404NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "No users found!" };
                return NotFound(_response);
            }

            var userList = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var firstRole = roles.FirstOrDefault() ?? "No Role Assigned";
                userList.Add(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    FirstRole = firstRole
                });
            }

            _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status200OK;
            _response.IsSuccess = true;
            _response.Result = userList;
            return Ok(_response);
        }


        /// <summary>
        ///
        /// </summary>
        /// 
        /// <param name="id"></param>
        /// 
        /// <param name="model"></param>
        [HttpPut("user/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="id"></param>
        ///
        /// <param name="model"></param>
        public async Task<ActionResult<ApiResponse>> UpdateUser(string id, [FromBody] UpdateUserDTO model)
        {
            if (string.IsNullOrEmpty(model.UserName) && string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Role))
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status400BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "At least one field (UserName, Email, or Role) must be provided for update!" };
                return BadRequest(_response);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);

            var userToUpdate = await _userManager.FindByIdAsync(id);
            if (userToUpdate == null)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status404NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "User not found!" };
                return NotFound(_response);
            }

            var userRoles = await _userManager.GetRolesAsync(userToUpdate);
            if (userRoles.Contains(SD.Role_Super_Admin) && !currentUserRoles.Contains(SD.Role_Super_Admin))
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status403Forbidden;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Access denied! Only Super Admin can update Super Admin data." };
                return StatusCode(StatusCodes.Status403Forbidden, _response);
            }

            if ((userRoles.Contains(SD.Role_Manager) || userRoles.Contains(SD.Role_User)) &&
                !currentUserRoles.Contains(SD.Role_Admin) && !currentUserRoles.Contains(SD.Role_Super_Admin))
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status403Forbidden;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Access denied! Only Admins can update Managers and Users." };
                return StatusCode(StatusCodes.Status403Forbidden, _response);
            }

            if (!string.IsNullOrEmpty(model.UserName))
            {
                userToUpdate.UserName = model.UserName;
            }

            if (!string.IsNullOrEmpty(model.Email))
            {
                userToUpdate.Email = model.Email;
            }

            var updateResult = await _userManager.UpdateAsync(userToUpdate);
            if (!updateResult.Succeeded)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status500InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = updateResult.Errors.Select(e => e.Description).ToList();
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }

            string? newRole = null;
            if (!string.IsNullOrEmpty(model.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(userToUpdate);

                if (currentRoles.Contains(SD.Role_Super_Admin) && !currentUserRoles.Contains(SD.Role_Super_Admin))
                {
                    _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status403Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "Access denied! Only Super Admin can change roles of another Super Admin." };
                    return StatusCode(StatusCodes.Status403Forbidden, _response);
                }

                if ((currentRoles.Contains(SD.Role_Manager) || currentRoles.Contains(SD.Role_User)) &&
                    !currentUserRoles.Contains(SD.Role_Admin) && !currentUserRoles.Contains(SD.Role_Super_Admin))
                {
                    _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status403Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "Access denied! Only Admins or Super Admins can change roles of Managers and Users." };
                    return StatusCode(StatusCodes.Status403Forbidden, _response);
                }

                await _userManager.RemoveFromRolesAsync(userToUpdate, currentRoles);
                await _userManager.AddToRoleAsync(userToUpdate, model.Role);
                newRole = model.Role;
            }

            _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status200OK;
            _response.IsSuccess = true;
            _response.Result = new
            {
                userToUpdate.Id,
                userToUpdate.UserName,
                userToUpdate.Email,
                NewRole = newRole ?? "No Role Change"
            };
            return Ok(_response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="id"></param>
        /// 
        [HttpDelete("user/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse>> DeleteUser(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);

            var userToDelete = await _userManager.FindByIdAsync(id);
            if (userToDelete == null)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status404NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "User not found!" };
                return NotFound(_response);
            }

            var userRoles = await _userManager.GetRolesAsync(userToDelete);

            // Super Admin cannot be deleted by anyone
            if (userRoles.Contains(SD.Role_Super_Admin))
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status403Forbidden;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Access denied! Super Admin cannot be deleted." };
                return StatusCode(StatusCodes.Status403Forbidden, _response);
            }

            // Admins can only delete Managers and Users
            if (currentUserRoles.Contains(SD.Role_Admin) &&
                (userRoles.Contains(SD.Role_Admin) || userRoles.Contains(SD.Role_Super_Admin)))
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status403Forbidden;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Access denied! Admins can only delete Managers and Users." };
                return StatusCode(StatusCodes.Status403Forbidden, _response);
            }

            try
            {
                var result = await _userManager.DeleteAsync(userToDelete);
                if (!result.Succeeded)
                {
                    _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status500InternalServerError;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = result.Errors.Select(e => e.Description).ToList();
                    return StatusCode(StatusCodes.Status500InternalServerError, _response);
                }

                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status200OK;
                _response.IsSuccess = true;
                _response.Result = new { Message = "User deleted successfully!" };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status500InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
    }
}
