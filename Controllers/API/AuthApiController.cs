using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeterReadingDash.Models.Dashboard;
using SmartMeterReadingDash.Services;
using System.Security.Claims;

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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
           [FromBody] LoginRequest request)
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

            var user = await _authRepository
                .GetUserByUsernameAsync(request.Username);

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

            // IMPORTANT:
            // Replace this with PasswordService verification
            if (request.Password != user.Password)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }

            await _authRepository
                .UpdateLastLoginAsync(user.UserId);

            var token = _jwtService.GenerateToken(user);

            Response.Cookies.Append(
                "SmartMeterAuth",
                token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !HttpContext.Request.IsHttps
                        ? false
                        : true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(60),

                    Path = "/"
                }
            );

            return Ok(new LoginResponse
            {
                Success = true,
                Token = "",
                ExpiresIn = 60,
                Username = user.Username,
                FullName = user.FullName ?? "",
                Role = user.Role
            });
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(
                "SmartMeterAuth",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                }
            );

            return Ok(new
            {
                success = true
            });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {


            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Username and password are required."
                });
            }

            if (request.Password.Length < 8)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password must contain at least 8 characters."
                });
            }

            var username = request.Username.Trim();

            var userCount = await _authRepository.GetUserCountAsync();

            var isInitialRegistration = userCount == 0;

            if (isInitialRegistration)
            {
                var usernameExists =
                    await _authRepository.UsernameExistsAsync(
                        username
                    );

                if (usernameExists)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Username already exists."
                    });
                }

                var superAdmin = new DashboardUser
                {
                    Username = username,

                    Password = request.Password,

                    FullName =
                        string.IsNullOrWhiteSpace(request.FullName)
                            ? null
                            : request.FullName.Trim(),

                    Role = "SUPERADMIN",

                    IsActive = 1,

                    Company = null,

                    Department = null
                };

                var userId = await _authRepository.CreateUserAsync(superAdmin);

                return Ok(new
                {
                    success = true,

                    message = "Initial SUPERADMIN created successfully.",

                    userId = userId,

                    username = superAdmin.Username,

                    fullName = superAdmin.FullName ?? "",

                    role = superAdmin.Role,

                    company = (string?)null,

                    department = (string?)null
                });
            }

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Only SUPERADMIN can create new users."
                });
            }

            if (!User.IsInRole("SUPERADMIN"))
            {
                return Forbid();
            }

            var requestedRole = string.IsNullOrWhiteSpace(request.Role)
                    ? "USER"
                    : request.Role.Trim().ToUpperInvariant();

            if (requestedRole != "ADMIN" && requestedRole != "USER")
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Only ADMIN or USER accounts can be created."
                });
            }


            if (string.IsNullOrWhiteSpace(request.Company))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Company is required for ADMIN and USER accounts."
                });
            }

            var company = request.Company.Trim().ToUpperInvariant();


            if (company != "BRPL" && company != "BYPL")
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Company must be either BRPL or BYPL."
                });
            }


            var department = string.IsNullOrWhiteSpace(request.Department)
                    ? null
                    : request.Department.Trim().ToUpperInvariant();


            var exists = await _authRepository.UsernameExistsAsync(username);

            if (exists)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Username already exists."
                });
            }


            var user = new DashboardUser
            {
                Username = username,

                Password = request.Password,

                FullName =
                    string.IsNullOrWhiteSpace(request.FullName)
                        ? null
                        : request.FullName.Trim(),

                Role = requestedRole,

                IsActive = 1,

                Company = company,

                Department = department
            };

            var newUserId = await _authRepository.CreateUserAsync(user);

            return Ok(new
            {
                success = true,

                message = $"{requestedRole} user created successfully.",

                userId = newUserId,

                username = user.Username,

                fullName = user.FullName ?? "",

                role = user.Role,

                company = user.Company,

                department = user.Department
            });
        }
        [Authorize]
        [HttpGet("my-access")]
        public IActionResult GetMyAccess()
        {
            return Ok(new
            {
                authenticated = User.Identity?.IsAuthenticated,

                userId =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier
                    )?.Value,

                username =
                    User.FindFirst(
                        ClaimTypes.Name
                    )?.Value,

                role =
                    User.FindFirst(
                        ClaimTypes.Role
                    )?.Value,

                company =
                    User.FindFirst("company")?.Value,

                department =
                    User.FindFirst("department")?.Value,

                isSuperAdmin =
                    User.IsInRole("SUPERADMIN"),

                isAdmin =
                    User.IsInRole("ADMIN")
            });
        }

    }
}