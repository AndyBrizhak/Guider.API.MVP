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
using System.Text.Json.Serialization;

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
        /// Authenticates a user based on the provided credentials and generates a JWT token.
        /// </summary>
        /// <param name="loginRequest">Login credentials containing username and password</param>
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest(new { message = "Username and password are required" });
            }

            string username = loginRequest.Username;
            string password = loginRequest.Password;

            // Поиск пользователя (проверяем и по имени пользователя, и по email)
            ApplicationUser userFromDb = _db.ApplicationUsers
                .FirstOrDefault(u =>
                    u.UserName.ToLower() == username.ToLower() ||
                    u.Email.ToLower() == username.ToLower());

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
            return Ok(new
            {
                token = tokenString,
                id = userFromDb.Id,
                username = userFromDb.UserName,
                email = userFromDb.Email,
                role = userRole.ToLower() // Роль в нижнем регистре для единообразия
            });
        }

        // Добавьте этот класс для приема данных от React Admin
        public class LoginRequest
        {
            [JsonPropertyName("username")]
            public string Username { get; set; }

            [JsonPropertyName("password")]
            public string Password { get; set; }
        }


        /// <summary>
        /// 
        /// Registers a new user based on the provided registration model.
        ///
        /// </summary>
        /// 
        /// <param name="requestModel">The registration request containing the following fields:
        /// 
        /// - username: The username of the new user.
        /// 
        /// - email: The email of the new user.
        /// 
        /// - password: The password of the new user.
        /// 
        /// - role: The role of the new user (optional, defaults to "user").
        /// 
        /// </param>

        [HttpPost("users")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateUser([FromBody] CreateUserData requestModel)
        {
            // Проверяем наличие обязательного объекта data
            if (requestModel == null)
            {
                return BadRequest(new { message = "Data object is required!" });
            }

            // Получаем данные из объекта data
            string userName = requestModel.username;
            string email = requestModel.email;
            string password = requestModel.password;
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
                        //data = new
                        //{
                            id = newUser.Id,
                            username = newUser.UserName,
                            email = newUser.Email,
                            role = userRole.ToLower()
                        //}
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

        public class CreateUserData
        {
            public string? username { get; set; }
            public string? email { get; set; }
            public string? password { get; set; }
            //public string role { get; set; }
        }


        /// <summary>
        /// Gets a paginated list of users based on the provided filters and sorting options.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="perPage"></param>
        /// <param name="sortField"></param>
        /// <param name="sortOrder"></param>
        /// <param name="username"></param>
        /// <param name="email"></param>
        /// <param name="id"></param>
        /// <param name="role"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet("users")]
        [Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<ActionResult> GetUsersPaged(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 10,
        [FromQuery] string sortField = "username",
        [FromQuery] string sortOrder = "ASC",
        [FromQuery] string username = null,
        [FromQuery] string email = null,
        [FromQuery] string id = null,
        [FromQuery] string role = null,
        [FromQuery] string filter = null) // Оставляем для обратной совместимости
        {
            // Получаем базовый запрос пользователей
            var usersQuery = _db.ApplicationUsers.AsQueryable();

            // Словарь для хранения фильтров
            Dictionary<string, string> filterDict = new Dictionary<string, string>();

            // Добавляем прямые параметры фильтрации (новый формат)
            if (!string.IsNullOrEmpty(username))
            {
                filterDict["username"] = username.ToLower();
            }

            if (!string.IsNullOrEmpty(email))
            {
                filterDict["email"] = email.ToLower();
            }

            if (!string.IsNullOrEmpty(id))
            {
                filterDict["id"] = id.ToLower();
            }

            if (!string.IsNullOrEmpty(role))
            {
                filterDict["role"] = role.ToLower();
            }

            // Для обратной совместимости проверяем фильтры в формате React Admin (filter[field]=value)
            var reactAdminFilters = HttpContext.Request.Query
                .Where(q => q.Key.StartsWith("filter[") && q.Key.EndsWith("]"))
                .ToDictionary(
                    q => q.Key.Substring(7, q.Key.Length - 8).ToLower(),
                    q => q.Value.ToString().ToLower()
                );

            // Добавляем React Admin фильтры в общий словарь
            foreach (var kvp in reactAdminFilters)
            {
                filterDict[kvp.Key] = kvp.Value;
            }

            // Для обратной совместимости проверяем, передан ли фильтр в формате JSON строки
            if (!string.IsNullOrEmpty(filter))
            {
                try
                {
                    // Пробуем десериализовать JSON
                    var jsonFilters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(filter);

                    // Добавляем JSON фильтры в общий словарь
                    foreach (var kvp in jsonFilters)
                    {
                        filterDict[kvp.Key.ToLower()] = kvp.Value.ToLower();
                    }
                }
                catch (Exception ex)
                {
                    // Если это не JSON, пробуем обработать как простую строку поиска по username
                    if (!string.IsNullOrEmpty(filter))
                    {
                        filterDict["username"] = filter.ToLower();
                    }

                    Console.WriteLine($"Error parsing JSON filter: {ex.Message}. Using filter as username search.");
                }
            }

            // Применяем все собранные фильтры к запросу (кроме role, который применяется позже)
            foreach (var kvp in filterDict)
            {
                string key = kvp.Key;
                string value = kvp.Value;

                if (!string.IsNullOrEmpty(value))
                {
                    if (key == "username")
                    {
                        usersQuery = usersQuery.Where(u => u.UserName.ToLower().Contains(value));
                    }
                    else if (key == "email")
                    {
                        usersQuery = usersQuery.Where(u => u.Email.ToLower().Contains(value));
                    }
                    else if (key == "id")
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
                Response.Headers.Append("X-Total-Count", "0");
                Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");
                return Ok(new List<object>());
            }

            // Извлекаем фильтр по роли из словаря
            string roleFilter = null;
            filterDict.TryGetValue("role", out roleFilter);

            // Формируем список пользователей с их ролями
            var userList = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var firstRole = roles.FirstOrDefault() ?? "user";

                // Фильтрация по роли если она указана в фильтре
                if (roleFilter != null && !firstRole.ToLower().Contains(roleFilter))
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

            // Добавляем заголовок с общим количеством записей
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            // Возвращаем данные в формате, ожидаемом React Admin
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
        [HttpGet("users/{id}")]
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
        /// - 403 Forbidden: Current user does not have permission to update the target user.
        /// 
        /// - 500 Internal Server Error: An error occurred during the update process.
        /// 
        /// </returns>
        /// 
        [HttpPut("users/{id}")]
        //[Authorize(Roles = SD.Role_Super_Admin + "," + SD.Role_Admin)]
        public async Task<ActionResult> UpdateUser(string id, [FromBody] UpdateUserData model)
        {
            // Проверка входных данных
            if (model == null)
            {
                return BadRequest(new { message = "Update data is required!" });
            }

            // Получаем данные из объекта data
            string userName = model.username;
            string email = model.email;
            string role = model.role;

            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(role))
            {
                return BadRequest(new { message = "At least one field (username, email, or role) must be provided for update!" });
            }

            var userToUpdate = await _userManager.FindByIdAsync(id);
            if (userToUpdate == null)
            {
                return NotFound(new { message = "User not found!" });
            }

            /* Закомментирован код проверки прав и ролей
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
            */

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
                //data = new
                //{
                    id = userToUpdate.Id,
                    username = userToUpdate.UserName,
                    email = userToUpdate.Email,
                    role = role.ToLower()
                //}
            });
        }
        public class UpdateUserData
        {
            public string? username { get; set; }
            public string? email { get; set; }
            public string? role { get; set; }
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
        [HttpDelete("users/{id}")]
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

            /* Закомментирован код проверки прав и ролей
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
            */


            // Удаление пользователя
            var result = await _userManager.DeleteAsync(userToDelete);
            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }

            // Возвращаем данные в формате, соответствующем DeleteResult
            return Ok(userData);
        }
        public class DeleteUserRequestDTO
        {
            public object? previousData { get; set; } // Предыдущие данные пользователя
            public object? meta { get; set; } // Опциональные метаданные
        }
    }
}
