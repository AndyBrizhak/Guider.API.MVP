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
    [Route("")]
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
        //[HttpPost("register")]
        //[ProducesResponseType(StatusCodes.Status201Created)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult> Register([FromBody] RegisterRequestDTO model)
        //{
        //    // Обработка данных в формате React Admin
        //    string userName = model.username;
        //    string email = model.email;
        //    string password = model.password;
        //    //string role = model.role;
        //    string role = SD.Role_User;



        //    // Валидация данных
        //    if (string.IsNullOrEmpty(email) || !new EmailAddressAttribute().IsValid(email))
        //    {
        //        return BadRequest(new { message = "A valid email address is required!" });
        //    }

        //    if (password.Length < 6)
        //    {
        //        return BadRequest(new { message = "Password must be at least 6 characters long!" });
        //    }

        //    if (_db.ApplicationUsers.Any(u => u.UserName.ToLower() == userName.ToLower()))
        //    {
        //        return BadRequest(new { message = "User already exists!" });
        //    }

        //    // Создание пользователя
        //    ApplicationUser newUser = new()
        //    {
        //        UserName = userName,
        //        Email = email,
        //        NormalizedUserName = userName.ToUpper(),
        //        NormalizedEmail = email.ToUpper(),
        //        EmailConfirmed = false,
        //        PhoneNumberConfirmed = false,
        //        TwoFactorEnabled = false,
        //        LockoutEnabled = true,
        //        SecurityStamp = Guid.NewGuid().ToString()
        //    };

        //    try
        //    {
        //        var result = await _userManager.CreateAsync(newUser, password);
        //        if (result.Succeeded)
        //        {
        //            // Создаем роли, если они не существуют
        //            if (!_roleManager.RoleExistsAsync(SD.Role_Super_Admin).GetAwaiter().GetResult())
        //            {
        //                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Super_Admin));
        //                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
        //                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Manager));
        //                await _roleManager.CreateAsync(new IdentityRole(SD.Role_User));
        //            }

        //            // Проверяем, существует ли указанная роль
        //            if (_roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
        //            {
        //                await _userManager.AddToRoleAsync(newUser, role);
        //            }
        //            else
        //            {
        //                // Если указанная роль не существует, назначаем роль по умолчанию
        //                await _userManager.AddToRoleAsync(newUser, SD.Role_User);
        //            }

        //            // Получаем роль пользователя для ответа
        //            var userRoles = await _userManager.GetRolesAsync(newUser);
        //            var userRole = userRoles.FirstOrDefault() ?? SD.Role_User;

        //            // Формируем ответ в формате, совместимом с React Admin
        //            return Created("", new
        //            {
        //                id = newUser.Id,
        //                username = newUser.UserName,
        //                email = newUser.Email,
        //                role = userRole.ToLower()
        //            });
        //        }
        //        else
        //        {
        //            // Обработка ошибок валидации
        //            var errors = result.Errors.Select(e => e.Description);
        //            return BadRequest(new { message = string.Join(", ", errors) });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        //    }
        //}

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Register([FromBody] CreateUserRequestDTO requestModel)
        {
            // Проверяем наличие обязательного объекта data
            if (requestModel?.data == null)
            {
                return BadRequest(new { message = "Data object is required!" });
            }

            // Получаем данные из объекта data
            string userName = requestModel.data.username;
            string email = requestModel.data.email;
            string password = requestModel.data.password;
            string role = /*!string.IsNullOrEmpty(requestModel.data.role) ? requestModel.data.role :*/ SD.Role_User;

            // Валидация данных
            if (string.IsNullOrEmpty(userName))
            {
                return BadRequest(new { message = "Username is required!" });
            }

            if (string.IsNullOrEmpty(email) || !new EmailAddressAttribute().IsValid(email))
            {
                return BadRequest(new { message = "A valid email address is required!" });
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
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

                    // Возвращаем ответ в формате, совместимом с React Admin (CreateResult)
                    return Created("", new
                    {
                        data = new
                        {
                            id = newUser.Id,
                            username = newUser.UserName,
                            email = newUser.Email,
                            role = userRole.ToLower()
                        }
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

        // DTO для запроса на создание пользователя в формате React Admin
        public class CreateUserRequestDTO
        {
            public CreateUserData data { get; set; }
            public object? meta { get; set; }
        }

        public class CreateUserData
        {
            public string username { get; set; }
            public string email { get; set; }
            public string password { get; set; }
            //public string role { get; set; }
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
        //[HttpGet("users")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        //public async Task<ActionResult> GetUsersPaged(int pageNumber = 1, int pageSize = 10)
        //{
        //    var users = _db.ApplicationUsers
        //        .Skip((pageNumber - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToList();

        //    if (!users.Any())
        //    {
        //        return NotFound(new { message = "No users found!" });
        //    }

        //    var userList = new List<object>();
        //    foreach (var user in users)
        //    {
        //        var roles = await _userManager.GetRolesAsync(user);
        //        var firstRole = roles.FirstOrDefault() ?? "No Role Assigned";
        //        userList.Add(new
        //        {
        //            id = user.Id,
        //            username = user.UserName,
        //            email = user.Email,
        //            role = firstRole.ToLower()
        //        });
        //    }

        //    Return data in format expected by React Admin
        //    return Ok(userList);
        //}

        [HttpGet("users")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<ActionResult> GetUsersPaged([FromQuery] int page = 1, [FromQuery] int perPage = 10,
        [FromQuery] string sortField = "username", [FromQuery] string sortOrder = "ASC",
        [FromQuery] string filter = "")
        {
            // Десериализация фильтра, если он предоставлен
            Dictionary<string, string> filterDict = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(filter))
            {
                try
                {
                    filterDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(filter);
                }
                catch
                {
                    // Если произошла ошибка при десериализации, оставляем пустой словарь
                }
            }

            // Получаем базовый запрос пользователей
            var usersQuery = _db.ApplicationUsers.AsQueryable();

            // Применяем фильтры
            if (filterDict != null)
            {
                foreach (var kvp in filterDict)
                {
                    string key = kvp.Key.ToLower();
                    string value = kvp.Value.ToLower();

                    if (key == "username" && !string.IsNullOrEmpty(value))
                    {
                        usersQuery = usersQuery.Where(u => u.UserName.ToLower().Contains(value));
                    }
                    else if (key == "email" && !string.IsNullOrEmpty(value))
                    {
                        usersQuery = usersQuery.Where(u => u.Email.ToLower().Contains(value));
                    }
                    else if (key == "id" && !string.IsNullOrEmpty(value))
                    {
                        usersQuery = usersQuery.Where(u => u.Id.ToLower().Contains(value));
                    }
                    // Фильтрация по роли будет применяться после получения данных
                }
            }

            // Получаем общее количество записей (до пагинации)
            int totalCount = usersQuery.Count();

            // Применяем сортировку
            sortField = sortField.ToLower();
            if (sortField == "username")
            {
                usersQuery = sortOrder.ToUpper() == "DESC"
                    ? usersQuery.OrderByDescending(u => u.UserName)
                    : usersQuery.OrderBy(u => u.UserName);
            }
            else if (sortField == "email")
            {
                usersQuery = sortOrder.ToUpper() == "DESC"
                    ? usersQuery.OrderByDescending(u => u.Email)
                    : usersQuery.OrderBy(u => u.Email);
            }
            else if (sortField == "id")
            {
                usersQuery = sortOrder.ToUpper() == "DESC"
                    ? usersQuery.OrderByDescending(u => u.Id)
                    : usersQuery.OrderBy(u => u.Id);
            }

            // Применяем пагинацию
            var users = usersQuery
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToList();

            if (!users.Any())
            {
                // React Admin ожидает пустой массив, а не 404
                return Ok(new
                {
                    data = new List<object>(),
                    total = 0
                });
            }

            // Формируем список пользователей с их ролями
            var userList = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var firstRole = roles.FirstOrDefault() ?? "user";

                // Фильтрация по роли если она указана в фильтре
                if (filterDict != null && filterDict.TryGetValue("role", out string roleFilter)
                    && !string.IsNullOrEmpty(roleFilter)
                    && !firstRole.ToLower().Contains(roleFilter.ToLower()))
                {
                    continue; // Пропускаем пользователя, если его роль не соответствует фильтру
                }

                userList.Add(new
                {
                    id = user.Id,
                    username = user.UserName,
                    email = user.Email,
                    role = firstRole.ToLower()
                });
            }

            // Возвращаем данные в формате, ожидаемом React Admin
            return Ok(new
            {
                data = userList,
                total = totalCount
            });
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
        //[HttpGet("user/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        //public async Task<ActionResult> GetUserById(string id)
        //{
        //    var user = await _userManager.FindByIdAsync(id);
        //    if (user == null)
        //    {
        //        return NotFound(new { message = "User not found!" });
        //    }

        //    var roles = await _userManager.GetRolesAsync(user);
        //    var userDetails = new
        //    {
        //        id = user.Id,
        //        username = user.UserName,
        //        email = user.Email,
        //        role = roles.FirstOrDefault()?.ToLower() ?? "user"
        //    };

        //    // Return data in format expected by React Admin
        //    return Ok(userDetails);
        //}

        [HttpGet("user/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
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

            // Возвращаем данные в формате, соответствующем GetOneResult
            return Ok(new
            {
                data = userDetails
            });
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
        /// - 403 Forbidden: Current user does not have permission to update the target user.
        /// 
        /// - 500 Internal Server Error: An error occurred during the update process.
        /// 
        /// </returns>
        /// 
        //[HttpPut("user/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        //public async Task<ActionResult> UpdateUser(string id, [FromBody] UpdateUserDTO model)
        //{
        //    // Get the UserName and Email from the React Admin format fields
        //    string userName = model.username;
        //    string email = model.email;
        //    string role = model.role;

        //    if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(role))
        //    {
        //        return BadRequest(new { message = "At least one field (UserName, Email, or Role) must be provided for update!" });
        //    }

        //    var userToUpdate = await _userManager.FindByIdAsync(id);
        //    if (userToUpdate == null)
        //    {
        //        return NotFound(new { message = "User not found!" });
        //    }

        //    // Get the roles of the current user (the one making the request)
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    if (currentUser == null)
        //    {
        //        return StatusCode(StatusCodes.Status403Forbidden,
        //        new { message = "You do not have permission to update this user's details!" });
        //    }

        //    var currentUserRoles = await _userManager.GetRolesAsync(currentUser);

        //    // Check if the current user is a Super Admin
        //    bool isSuperAdmin = currentUserRoles.Contains(SD.Role_Super_Admin);

        //    if (!isSuperAdmin)
        //    {
        //        // If not a Super Admin, ensure the target user has a role of "user" or "manager"
        //        var targetUserRoles = await _userManager.GetRolesAsync(userToUpdate);
        //        if (targetUserRoles.Any(r => r.Equals(SD.Role_Super_Admin, StringComparison.OrdinalIgnoreCase) ||
        //                                     r.Equals(SD.Role_Admin, StringComparison.OrdinalIgnoreCase)))
        //        {
        //            return StatusCode(StatusCodes.Status403Forbidden,
        //            new { message = "You do not have permission to update this user's details!" });
        //        }

        //        if (!string.IsNullOrEmpty(role) &&
        //        (role.Equals(SD.Role_Super_Admin, StringComparison.OrdinalIgnoreCase) ||
        //         role.Equals(SD.Role_Admin, StringComparison.OrdinalIgnoreCase)))
        //        {
        //            return StatusCode(StatusCodes.Status403Forbidden,
        //            new { message = "Only Super Admins can assign Admin or Super Admin roles!" });
        //        }
        //    }

        //    if (!string.IsNullOrEmpty(userName))
        //    {
        //        userToUpdate.UserName = userName;
        //    }

        //    if (!string.IsNullOrEmpty(email))
        //    {
        //        userToUpdate.Email = email;
        //    }

        //    var updateResult = await _userManager.UpdateAsync(userToUpdate);
        //    if (!updateResult.Succeeded)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new { message = string.Join(", ", updateResult.Errors.Select(e => e.Description)) });
        //    }

        //    if (!string.IsNullOrEmpty(role))
        //    {
        //        var currentRoles = await _userManager.GetRolesAsync(userToUpdate);
        //        await _userManager.RemoveFromRolesAsync(userToUpdate, currentRoles);
        //        await _userManager.AddToRoleAsync(userToUpdate, role);
        //    }

        //    // Return data in format expected by React Admin
        //    return Ok(new
        //    {
        //        id = userToUpdate.Id,
        //        username = userToUpdate.UserName,
        //        email = userToUpdate.Email,
        //        role = role ?? "user"
        //    });
        //}

        [HttpPut("user/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<ActionResult> UpdateUser(string id, [FromBody] UpdateUserRequestDTO model)
        {
            // Проверка входных данных
            if (model?.data == null)
            {
                return BadRequest(new { message = "Update data is required!" });
            }

            // Получаем данные из объекта data
            string userName = model.data.username;
            string email = model.data.email;
            string role = model.data.role;

            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(role))
            {
                return BadRequest(new { message = "At least one field (username, email, or role) must be provided for update!" });
            }

            var userToUpdate = await _userManager.FindByIdAsync(id);
            if (userToUpdate == null)
            {
                return NotFound(new { message = "User not found!" });
            }

            // Проверка прав доступа
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You do not have permission to update this user's details!" });
            }

            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            bool isSuperAdmin = currentUserRoles.Contains(SD.Role_Super_Admin);

            if (!isSuperAdmin)
            {
                // Если не Super Admin, проверяем, что целевой пользователь имеет роль "user" или "manager"
                var targetUserRoles = await _userManager.GetRolesAsync(userToUpdate);
                if (targetUserRoles.Any(r => r.Equals(SD.Role_Super_Admin, StringComparison.OrdinalIgnoreCase) ||
                                             r.Equals(SD.Role_Admin, StringComparison.OrdinalIgnoreCase)))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "You do not have permission to update this user's details!" });
                }

                if (!string.IsNullOrEmpty(role) &&
                (role.Equals(SD.Role_Super_Admin, StringComparison.OrdinalIgnoreCase) ||
                 role.Equals(SD.Role_Admin, StringComparison.OrdinalIgnoreCase)))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Only Super Admins can assign Admin or Super Admin roles!" });
                }
            }

            // Обновление данных пользователя
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

            // Обновление роли, если она указана
            if (!string.IsNullOrEmpty(role))
            {
                var currentRoles = await _userManager.GetRolesAsync(userToUpdate);
                await _userManager.RemoveFromRolesAsync(userToUpdate, currentRoles);
                await _userManager.AddToRoleAsync(userToUpdate, role);
            }
            else
            {
                // Если роль не указана в запросе, используем текущую роль
                var currentRoles = await _userManager.GetRolesAsync(userToUpdate);
                role = currentRoles.FirstOrDefault() ?? "user";
            }

            // Формируем ответ в формате, соответствующем UpdateResult
            return Ok(new
            {
                data = new
                {
                    id = userToUpdate.Id,
                    username = userToUpdate.UserName,
                    email = userToUpdate.Email,
                    role = role.ToLower()
                }
            });
        }

        // DTO для запроса на обновление пользователя в формате React Admin
        public class UpdateUserRequestDTO
        {
            public UpdateUserData data { get; set; }
            public object? previousData { get; set; } // Может содержать предыдущие данные, не требуется для обработки
            public object? meta { get; set; } // Опциональные метаданные
        }

        public class UpdateUserData
        {
            public string username { get; set; }
            public string email { get; set; }
            public string role { get; set; }
        }

        /// <summary>
        /// Deletes a user based on the provided unique identifier.
        /// 
        /// </summary>
        /// 
        /// <param name="id">The unique identifier of the user to delete.</param>
        /// 
        /// <returns>
        /// 
        /// Returns a success message in the format expected by React Admin.
        /// 
        /// Possible outcomes:
        /// 
        /// - 200 OK: User deleted successfully.
        /// 
        /// - 404 Not Found: User with the specified ID does not exist.
        /// 
        /// - 500 Internal Server Error: An error occurred during the deletion process.
        /// 
        /// </returns>
        //[HttpDelete("user/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        //public async Task<ActionResult> DeleteUser(string id)
        //{
        //    var userToDelete = await _userManager.FindByIdAsync(id);
        //    if (userToDelete == null)
        //    {
        //        return NotFound(new { message = "User not found!" });
        //    }

        //    // Get the roles of the current user (the one making the request)
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    if (currentUser == null)
        //    {
        //        return StatusCode(StatusCodes.Status403Forbidden,
        //        new { message = "You do not have permission to delete this user!" });
        //    }

        //    var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
        //    // Check if the current user is a Super Admin
        //    bool isSuperAdmin = currentUserRoles.Contains(SD.Role_Super_Admin);

        //    if (!isSuperAdmin)
        //    {
        //        // If not a Super Admin, ensure the target user has a role of "user" or "manager" only
        //        var targetUserRoles = await _userManager.GetRolesAsync(userToDelete);
        //        if (targetUserRoles.Any(r => r.Equals(SD.Role_Super_Admin, StringComparison.OrdinalIgnoreCase) ||
        //                                     r.Equals(SD.Role_Admin, StringComparison.OrdinalIgnoreCase)))
        //        {
        //            return StatusCode(StatusCodes.Status403Forbidden,
        //            new { message = "Only Super Admins can delete users with Admin or Super Admin roles!" });
        //        }
        //    }


        //    var result = await _userManager.DeleteAsync(userToDelete);
        //    if (!result.Succeeded)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        //    }

        //    // Return success message in format expected by React Admin
        //    return Ok(new { id = id });
        //}

        [HttpDelete("user/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<ActionResult> DeleteUser(string id, [FromBody] DeleteUserRequestDTO model = null)
        {
            var userToDelete = await _userManager.FindByIdAsync(id);
            if (userToDelete == null)
            {
                return NotFound(new { message = "User not found!" });
            }

            // Получаем роли пользователя, прежде чем удалить его
            var userRoles = await _userManager.GetRolesAsync(userToDelete);
            var userRole = userRoles.FirstOrDefault() ?? "user";

            // Сохраняем данные пользователя, чтобы вернуть их после удаления
            var userData = new
            {
                id = userToDelete.Id,
                username = userToDelete.UserName,
                email = userToDelete.Email,
                role = userRole.ToLower()
            };

            // Проверка прав доступа
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You do not have permission to delete this user!" });
            }

            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            // Проверяем, является ли текущий пользователь Super Admin
            bool isSuperAdmin = currentUserRoles.Contains(SD.Role_Super_Admin);

            if (!isSuperAdmin)
            {
                // Если не Super Admin, убеждаемся, что целевой пользователь имеет роль только "user" или "manager"
                if (userRoles.Any(r => r.Equals(SD.Role_Super_Admin, StringComparison.OrdinalIgnoreCase) ||
                                        r.Equals(SD.Role_Admin, StringComparison.OrdinalIgnoreCase)))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Only Super Admins can delete users with Admin or Super Admin roles!" });
                }
            }

            // Удаление пользователя
            var result = await _userManager.DeleteAsync(userToDelete);
            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }

            // Возвращаем данные в формате, соответствующем DeleteResult
            return Ok(new
            {
                data = userData
            });
        }

        // DTO для запроса на удаление пользователя в формате React Admin
        public class DeleteUserRequestDTO
        {
            public object previousData { get; set; } // Предыдущие данные пользователя
            public object meta { get; set; } // Опциональные метаданные
        }
    }
}
