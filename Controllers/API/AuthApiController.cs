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


        // =========================================================
        // LOGIN
        // POST: /api/AuthApi/login
        // =========================================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            // -----------------------------------------------------
            // Validate request
            // -----------------------------------------------------

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


            // -----------------------------------------------------
            // Find user
            // -----------------------------------------------------

            var user =
                await _authRepository.GetUserByUsernameAsync(
                    request.Username
                );


            // -----------------------------------------------------
            // User not found
            // -----------------------------------------------------

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Invalid username or password."
                });
            }


            // -----------------------------------------------------
            // Check active
            // -----------------------------------------------------

            if (user.IsActive != 1)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "User account is inactive."
                });
            }


            // -----------------------------------------------------
            // Password validation
            // -----------------------------------------------------
            // NOTE:
            // Password is currently stored directly in Oracle.
            // -----------------------------------------------------

            if (request.Password != user.Password)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Invalid username or password."
                });
            }


            // -----------------------------------------------------
            // Update last login
            // -----------------------------------------------------

            await _authRepository.UpdateLastLoginAsync(
                user.UserId
            );


            // -----------------------------------------------------
            // Generate JWT
            // -----------------------------------------------------

            var token =
       _jwtService.GenerateToken(user);


            // =========================================================
            // STORE JWT IN HTTP-ONLY COOKIE
            // =========================================================

            Response.Cookies.Append(
                "SmartMeterAuth",
                token,
                new CookieOptions
                {
                    HttpOnly = true,

                    Secure = true,

                    SameSite = SameSiteMode.Strict,

                    Expires =
                        DateTimeOffset.UtcNow.AddMinutes(60),

                    IsEssential = true
                }
            );


            return Ok(new
            {
                success = true,

                username = user.Username,

                fullName =
                    user.FullName ?? "",

                role = user.Role,

                expiresIn = 60
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
            // -----------------------------------------------------
            // Validate request
            // -----------------------------------------------------

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


            // -----------------------------------------------------
            // Password length
            // -----------------------------------------------------

            if (request.Password.Length < 8)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Password must contain at least 8 characters."
                });
            }


            // -----------------------------------------------------
            // Check existing users
            // -----------------------------------------------------

            var userCount =
                await _authRepository.GetUserCountAsync();


            // -----------------------------------------------------
            // FIRST USER ONLY
            // -----------------------------------------------------

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