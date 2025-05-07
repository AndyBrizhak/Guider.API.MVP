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
        /// 
        /// Authenticates a user based on the provided credentials and generates a JWT token.
        /// 
        /// </summary>
        /// 
        /// <param name="model">The login request containing the following fields:
        /// 
        /// - UserName: The username of the user.
        /// 
        /// - Password: The password of the user.
        /// 
        /// </param>
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequestDTO model)
        {
            // React Admin по умолчанию отправляет username и password
            // Проверяем, не пришли ли данные в формате React Admin
            string userName = model.username;
            string password = model.password;

            // Если в модели нет UserName, но есть Email или Username (как в React Admin)
            if (string.IsNullOrEmpty(userName))
            {
                // React Admin может отправить email в качестве username
                if (!string.IsNullOrEmpty(model.Email))
                {
                    userName = model.Email;
                }
                // Или может использовать поле username в нижнем регистре
                else if (model.GetType().GetProperty("username") != null)
                {
                    userName = (string)model.GetType().GetProperty("username").GetValue(model, null);
                }
            }

            // Если в модели нет Password, но есть password (в нижнем регистре, как в React Admin)
            //if (string.IsNullOrEmpty(password) && model.GetType().GetProperty("password") != null)
            //{
            //    password = (string)model.GetType().GetProperty("password").GetValue(model, null);
            //}

            // Поиск пользователя (проверяем и по имени пользователя, и по email)
            ApplicationUser userFromDb = _db.ApplicationUsers
                .FirstOrDefault(u =>
                    u.UserName.ToLower() == userName.ToLower() ||
                    u.Email.ToLower() == userName.ToLower());

            // Если пользователь не найден или пароль неверный
            if (userFromDb == null || !await _userManager.CheckPasswordAsync(userFromDb, password))
            {
                return Unauthorized(new
                {
                    message = "Username or password is incorrect!"
                });
            }

            // Получаем роли пользователя
            var userRoles = await _userManager.GetRolesAsync(userFromDb);
            var userRole = userRoles.FirstOrDefault() ?? "user";

            // Создаем токен
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

            // Формируем ответ в формате, подходящем для React Admin
            // React Admin ожидает прямой ответ без обертки
            return Ok(new
            {
                token = tokenString,
                id = userFromDb.Id,
                username = userFromDb.UserName,
                email = userFromDb.Email,
                role = userRole.ToLower() // Роль в нижнем регистре для единообразия
            });
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Register([FromBody] RegisterRequestDTO model)
        {
            // Обработка данных в формате React Admin
            string userName = model.username;
            string email = model.email;
            string password = model.password;
            //string role = model.role;
            string role = SD.Role_User;

            // Если оригинальные поля пустые, используем поля в формате React Admin
            //if (string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(model.username))
            //{
            //    userName = model.username;
            //}

            //if (string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(model.email))
            //{
            //    email = model.email;
            //}

            //if (string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(model.password))
            //{
            //    password = model.password;
            //}

            //if (string.IsNullOrEmpty(role) && !string.IsNullOrEmpty(model.role))
            //{
            //    role = model.role;
            //}

            // Если роль не указана, устанавливаем значение по умолчанию
            //if (string.IsNullOrEmpty(role))
            //{
            //    role = SD.Role_User;
            //}

            // Валидация данных
            if (string.IsNullOrEmpty(email) || !new EmailAddressAttribute().IsValid(email))
            {
                return BadRequest(new { message = "A valid email address is required!" });
            }

            if (password.Length < 6)
            {
                return BadRequest(new { message = "Password must be at least 6 characters long!" });
            }

            if (_db.ApplicationUsers.Any(u => u.UserName.ToLower() == userName.ToLower()))
            {
                return BadRequest(new { message = "User already exists!" });
            }

            // Создание пользователя
            ApplicationUser newUser = new()
            {
                UserName = userName,
                Email = email,
                NormalizedUserName = userName.ToUpper(),
                NormalizedEmail = email.ToUpper(),
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            try
            {
                var result = await _userManager.CreateAsync(newUser, password);
                if (result.Succeeded)
                {
                    // Создаем роли, если они не существуют
                    if (!_roleManager.RoleExistsAsync(SD.Role_Super_Admin).GetAwaiter().GetResult())
                    {
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Super_Admin));
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Manager));
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_User));
                    }

                    // Проверяем, существует ли указанная роль
                    if (_roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
                    {
                        await _userManager.AddToRoleAsync(newUser, role);
                    }
                    else
                    {
                        // Если указанная роль не существует, назначаем роль по умолчанию
                        await _userManager.AddToRoleAsync(newUser, SD.Role_User);
                    }

                    // Получаем роль пользователя для ответа
                    var userRoles = await _userManager.GetRolesAsync(newUser);
                    var userRole = userRoles.FirstOrDefault() ?? SD.Role_User;

                    // Формируем ответ в формате, совместимом с React Admin
                    return Created("", new
                    {
                        id = newUser.Id,
                        username = newUser.UserName,
                        email = newUser.Email,
                        role = userRole.ToLower()
                    });
                }
                else
                {
                    // Обработка ошибок валидации
                    var errors = result.Errors.Select(e => e.Description);
                    return BadRequest(new { message = string.Join(", ", errors) });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        /// <summary>
        /// 
        /// Retrieves a paginated list of users.
        /// 
        /// </summary>
        /// 
        /// <param name="pageNumber">The page number to retrieve (default is 1).</param>
        /// 
        /// <param name="pageSize">The number of users per page (default is 10).</param>
        /// 
        /// <returns>
        /// 
        /// Returns a paginated list of users in the format expected by React Admin.
        /// 
        /// Possible outcomes:
        /// 
        /// - 200 OK: Users retrieved successfully.
        /// 
        /// - 404 Not Found: No users found.
        /// 
        /// </returns>
        [HttpGet("users")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<ActionResult> GetUsersPaged(int pageNumber = 1, int pageSize = 10)
        {
            var users = _db.ApplicationUsers
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            if (!users.Any())
            {
                return NotFound(new { message = "No users found!" });
            }

            var userList = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var firstRole = roles.FirstOrDefault() ?? "No Role Assigned";
                userList.Add(new
                {
                    id = user.Id,
                    username = user.UserName,
                    email = user.Email,
                    role = firstRole.ToLower()
                });
            }

            // Return data in format expected by React Admin
            return Ok(userList);
        }

          
        /// <summary>
        /// 
        /// Retrieves a user by their unique identifier.
        /// 
        /// </summary>
        /// 
        /// <param name="id">The unique identifier of the user.</param>
        /// 
        /// <returns>
        /// 
        /// Returns the user's details in the format expected by React Admin.
        /// 
        /// Possible outcomes:
        /// 
        /// - 200 OK: User found and details retrieved successfully.
        /// 
        /// - 404 Not Found: User with the specified ID does not exist.
        /// 
        /// </returns>
        [HttpGet("user/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<ActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found!" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDetails = new
            {
                id = user.Id,
                username = user.UserName,
                email = user.Email,
                role = roles.FirstOrDefault()?.ToLower() ?? "user"
            };

            // Return data in format expected by React Admin
            return Ok(userDetails);
        }

        /// <summary>
        /// 
        /// Updates a user's details based on the provided unique identifier and update model.
        /// 
        /// </summary>
        /// 
        /// <param name="id">The unique identifier of the user to update.</param>
        /// 
        /// <param name="model">The update model containing the following fields:
        /// 
        /// - UserName: The new username of the user (optional).
        /// 
        /// - Email: The new email of the user (optional).
        /// 
        /// - Role: The new role of the user (optional).
        /// 
        /// </param>
        /// 
        /// <returns>
        /// 
        /// Returns the updated user's details in the format expected by React Admin.
        /// 
        /// Possible outcomes:
        /// 
        /// - 200 OK: User updated successfully.
        /// 
        /// - 400 Bad Request: At least one field (UserName, Email, or Role) must be provided for update.
        /// 
        /// - 404 Not Found: User with the specified ID does not exist.
        /// 
        /// - 500 Internal Server Error: An error occurred during the update process.
        /// 
        /// </returns>
        /// 
        [HttpPut("user/{id}")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<ActionResult> UpdateUser(string id, [FromBody] UpdateUserDTO model)
        {
            // Get the UserName and Email from the React Admin format fields
            string userName =  model.username;
            string email =  model.email;
            string role =  model.role;

            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(role))
            {
                return BadRequest(new { message = "At least one field (UserName, Email, or Role) must be provided for update!" });
            }

            var userToUpdate = await _userManager.FindByIdAsync(id);
            if (userToUpdate == null)
            {
                return NotFound(new { message = "User not found!" });
            }

            if (!string.IsNullOrEmpty(userName))
            {
                userToUpdate.UserName = userName;
            }

            if (!string.IsNullOrEmpty(email))
            {
                userToUpdate.Email = email;
            }

            var updateResult = await _userManager.UpdateAsync(userToUpdate);
            if (!updateResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = string.Join(", ", updateResult.Errors.Select(e => e.Description)) });
            }

            if (!string.IsNullOrEmpty(role))
            {
                var currentRoles = await _userManager.GetRolesAsync(userToUpdate);
                await _userManager.RemoveFromRolesAsync(userToUpdate, currentRoles);
                await _userManager.AddToRoleAsync(userToUpdate, role);
            }

            // Return data in format expected by React Admin
            return Ok(new
            {
                id = userToUpdate.Id,
                username = userToUpdate.UserName,
                email = userToUpdate.Email,
                role = role ?? "user"
            });
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
