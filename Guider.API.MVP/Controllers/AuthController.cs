using Guider.API.MVP.Data;
using Guider.API.MVP.Models;
using Guider.API.MVP.Models.Dto;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
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

        /// <summary>
        /// Authenticates a user based on the provided credentials and generates a JWT token.
        /// </summary>
        /// <param name="model">The login request containing the following fields:
        /// - UserName: The username of the user.
        /// - Password: The password of the user.
        /// </param>
        /// <returns>
        /// Returns an ApiResponse containing the login result. If successful, the response includes:
        /// - UserId: The unique identifier of the user.
        /// - UserName: The username of the user.
        /// - Email: The email address of the user.
        /// - Token: The generated JWT access token.
        /// Possible status codes:
        /// - 200 OK: Login successful.
        /// - 400 Bad Request: Invalid request or incorrect username/password.
        /// - 401 Unauthorized: Authentication failed.
        /// - 500 Internal Server Error: An error occurred during processing.
        /// </returns>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse>> Login([FromBody] LoginRequestDTO model)
        {
            ApplicationUser userFromDb = _db.ApplicationUsers
                .FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());

            if (userFromDb == null || !await _userManager.CheckPasswordAsync(userFromDb, model.Password))
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status401Unauthorized;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Username or password is incorrect!" };
                return BadRequest(_response);
            }

            var userRoles = await _userManager.GetRolesAsync(userFromDb);
            var userRole = userRoles.FirstOrDefault() ?? "User";

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
                Token = tokenString
            };

            _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status200OK;
            _response.IsSuccess = true;
            _response.Result = loginResponse;
            return Ok(_response);
            
        }



        
        ///<sumary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="model">The registration request containing the following fields:
        /// - UserName: The username of the user (required).
        /// - Email: The email address of the user (required, must be valid).
        /// - Password: The password of the user (required, must be at least 6 characters long).
        /// </param>
        /// <returns>
        /// Returns an ApiResponse indicating the result of the registration process. Possible outcomes:
        /// - 201 Created: User registered successfully.
        /// - 400 Bad Request: Validation errors such as invalid email, short password, or username already exists.
        /// - 500 Internal Server Error: An error occurred during the registration process.
        /// </returns>
        /// <remarks>
        /// The email must be a valid email address, and the password must be at least 6 characters long.
        /// </remarks>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse))]
        public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterRequestDTO model)
        {
            if (string.IsNullOrEmpty(model.Email) || !new EmailAddressAttribute().IsValid(model.Email))
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status400BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "A valid email address is required!" };
                return BadRequest(_response);
            }

            if (model.Password.Length < 6)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status400BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "Password must be at least 6 characters long!" };
                return BadRequest(_response);
            }

            if (_db.ApplicationUsers.Any(u => u.UserName.ToLower() == model.UserName.ToLower()))
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
                    //_response.Result = newUser;
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
        /// Retrieves a paginated list of users from the database.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve. Defaults to 1.</param>
        /// <param name="pageSize">The number of users per page. Defaults to 10.</param>
        /// <returns>
        /// Returns an ApiResponse containing the paginated list of users. Each user includes:
        /// - Id: The user's unique identifier.
        /// - UserName: The user's username.
        /// - Email: The user's email address.
        /// - FirstRole: The first role assigned to the user or "No Role Assigned" if none exist.
        /// Possible status codes:
        /// - 200 OK: Users retrieved successfully.
        /// - 404 Not Found: No users found.
        /// </returns>
        /// <remarks>
        /// This method is restricted to users with the "Super Admin" or "Admin" roles.
        /// </remarks>
        [HttpGet("users")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse>> GetUsersPaged(int pageNumber = 1, int pageSize = 10)
        {
            var users = _db.ApplicationUsers
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            if (!users.Any())
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
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>
        /// Returns an ApiResponse containing the user's details if found. Possible outcomes:
        /// - 200 OK: User found and details retrieved successfully.
        /// - 404 Not Found: User with the specified ID does not exist.
        /// </returns>
        /// <remarks>
        /// This method is restricted to users with the "Super Admin" or "Admin" roles.
        /// </remarks>
        [HttpGet("user/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse>> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status404NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "User not found!" };
                return NotFound(_response);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDetails = new
            {
                user.Id,
                user.UserName,
                user.Email,
                Roles = roles
            };

            _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status200OK;
            _response.IsSuccess = true;
            _response.Result = userDetails;
            return Ok(_response);
        }


        /// <summary>
        /// Updates a user's details, including their username, email, or role.
        /// </summary>
        /// <param name="id">The unique identifier of the user to update.</param>
        /// <param name="model">The update request containing the new username, email, or role.</param>
        /// <returns>
        /// Returns an ApiResponse indicating the result of the update operation. Possible outcomes:
        /// - 200 OK: User updated successfully.
        /// - 400 Bad Request: No fields provided for update or validation errors.
        /// - 404 Not Found: User with the specified ID does not exist.
        /// - 500 Internal Server Error: An error occurred during the update process.
        /// </returns>
        /// <remarks>
        /// This method is restricted to users with the "Super Admin" or "Admin" roles.
        /// </remarks>
        [HttpPut("user/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse>> UpdateUser(string id, [FromBody] UpdateUserDTO model)
        {
            if (string.IsNullOrEmpty(model.UserName) && string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Role))
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status400BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "At least one field (UserName, Email, or Role) must be provided for update!" };
                return BadRequest(_response);
            }

            var userToUpdate = await _userManager.FindByIdAsync(id);
            if (userToUpdate == null)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status404NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "User not found!" };
                return NotFound(_response);
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

            if (!string.IsNullOrEmpty(model.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(userToUpdate);
                await _userManager.RemoveFromRolesAsync(userToUpdate, currentRoles);
                await _userManager.AddToRoleAsync(userToUpdate, model.Role);
            }

            _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status200OK;
            _response.IsSuccess = true;
            _response.Result = new
            {
                userToUpdate.Id,
                userToUpdate.UserName,
                userToUpdate.Email,
                NewRole = model.Role ?? "No Role Change"
            };
            return Ok(_response);
        }


        /// <summary>
        /// Deletes a user by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user to delete.</param>
        /// <returns>
        /// Returns an ApiResponse indicating the result of the delete operation. Possible outcomes:
        /// - 200 OK: User deleted successfully.
        /// - 404 Not Found: User with the specified ID does not exist.
        /// - 500 Internal Server Error: An error occurred during the delete process.
        /// </returns>
        /// <remarks>
        /// This method is restricted to users with the "Super Admin" or "Admin" roles.
        /// </remarks>
        [HttpDelete("user/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse>> DeleteUser(string id)
        {
            var userToDelete = await _userManager.FindByIdAsync(id);
            if (userToDelete == null)
            {
                _response.StatusCode = (System.Net.HttpStatusCode)StatusCodes.Status404NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { "User not found!" };
                return NotFound(_response);
            }

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
    }
}
