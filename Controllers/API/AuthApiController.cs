using Microsoft.AspNetCore.Mvc;
using SmartMeterReadingDash.Models.Dashboard;
using SmartMeterReadingDash.Services;

namespace SmartMeterReadingDash.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly AuthRepository _authRepository;
        private readonly JwtService _jwtService;

        public AuthApiController(
            AuthRepository authRepository,
            JwtService jwtService)
        {
            _authRepository = authRepository;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
         [FromBody] Models.Dashboard.LoginRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Username and password are required."
                });
            }

            var user =
                await _authRepository.GetUserByUsernameAsync(
                    request.Username
                );

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }

            if (user.IsActive != 1)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User account is inactive."
                });
            }

            if (request.Password != user.Password)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }

            await _authRepository.UpdateLastLoginAsync(
                user.UserId
            );

            var token =
                _jwtService.GenerateToken(user);

            Response.Cookies.Append(
                "SmartMeterAuth",
                token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(60),
                    Path = "/"
                }
            );

            return Ok(new LoginResponse
            {
                Success = true,
                Token = "", // Don't expose JWT to JavaScript
                ExpiresIn = 60,
                Username = user.Username,
                FullName = user.FullName ?? "",
                Role = user.Role
            });
        }
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(
                "SmartMeterAuth",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                }
            );

            return Ok(new
            {
                success = true,
                message = "Logged out successfully."
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Username and password are required."
                });
            }

            if (request.Password.Length < 8)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Password must contain at least 8 characters."
                });
            }


            var userCount =
                await _authRepository.GetUserCountAsync();

            if (userCount > 0)
            {
                return Unauthorized(new
                {
                    success = false,

                    message =
                        "Registration is restricted to administrators."
                });
            }


            var username =
                request.Username.Trim();

            var usernameExists =
                await _authRepository.UsernameExistsAsync(
                    username
                );

            if (usernameExists)
            {
                return Conflict(new
                {
                    success = false,

                    message =
                        "Username already exists."
                });
            }

            var user = new DashboardUser
            {
                Username = username,

                Password = request.Password,

                FullName =
                    string.IsNullOrWhiteSpace(
                        request.FullName)
                        ? null
                        : request.FullName.Trim(),

                Role = "ADMIN",

                IsActive = 1
            };

            var userId =
                await _authRepository.CreateUserAsync(
                    user
                );

            return Ok(new
            {
                success = true,

                message =
                    "Initial administrator created successfully.",

                userId = userId,

                username = username,

                fullName =
                    user.FullName ?? "",

                role = "ADMIN"
            });
        }
    }
}