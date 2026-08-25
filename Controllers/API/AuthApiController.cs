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

            if (request.Password != user.Password)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }

            await _authRepository.UpdateLastLoginAsync(user.UserId);

            var token = _jwtService.GenerateToken(user);

            Response.Cookies.Append( "SmartMeterAuth", token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
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

        [HttpPost("register")]
        public async Task<IActionResult> Register( [FromBody] RegisterRequest request)
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

            var userCount =  await _authRepository.GetUserCountAsync();

            if (userCount == 0)
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

                    FullName =  string.IsNullOrWhiteSpace(request.FullName)
                            ? null
                            : request.FullName.Trim(),

                    Role = "SUPERADMIN",
                    Company = null,
                    Department = null,
                    IsActive = 1
                };

                var superAdminId =  await _authRepository.CreateUserAsync( superAdmin);

                return Ok(new
                {
                    success = true,
                    message = "Initial SUPERADMIN created successfully.",
                    userId = superAdminId,
                    username = superAdmin.Username,
                    fullName = superAdmin.FullName ?? "",
                    role = superAdmin.Role,
                    company = superAdmin.Company,
                    department = superAdmin.Department
                });
            }

            if (User?.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new
                {
                    success = false,

                    message = "You must be logged in to create users."
                });
            }


            var creatorRole = User.FindFirst(ClaimTypes.Role)?.Value
                              ?.Trim()
                              .ToUpperInvariant();

            var creatorCompany =  User.FindFirst("company")?.Value
                                  ?.Trim()
                                  .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Role is required."
                });
            }

            var requestedRole = request.Role.Trim().ToUpperInvariant();


            if (requestedRole != "SUPERADMIN" && requestedRole != "COMPANY_ADMIN" && requestedRole != "USER")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid role. Allowed roles are: " + "SUPERADMIN, COMPANY_ADMIN, USER."
                });
            }

            var requestedCompany = string.IsNullOrWhiteSpace(request.Company)
                                ? null
                                : request.Company.Trim().ToUpperInvariant();

            var requestedDepartment = string.IsNullOrWhiteSpace(request.Department)
                                ? null
                                : request.Department.Trim().ToUpperInvariant();


            if (creatorRole == "SUPERADMIN")
            {
                // SUPERADMIN can create all valid roles.
            }
            else if (creatorRole == "COMPANY_ADMIN")
            {
                if (requestedRole != "USER")
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new
                        {
                            success = false,

                            message =
                                "COMPANY_ADMIN can create USER only."
                        });
                }

                if (string.IsNullOrWhiteSpace(creatorCompany))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new
                        {
                            success = false,

                            message = "Your account does not have a company assigned."
                        });
                }

                requestedCompany = creatorCompany;
            }
            else
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,

                        message = "You do not have permission to create users."
                    });
            }

            if (requestedRole == "SUPERADMIN")
            {
                if (!string.IsNullOrWhiteSpace(requestedCompany) ||
                    !string.IsNullOrWhiteSpace(requestedDepartment))
                {
                    return BadRequest(new
                    {
                        success = false,

                        message = "SUPERADMIN cannot have Company or Department."
                    });
                }
            }

            if (requestedRole == "COMPANY_ADMIN")
            {
                if (string.IsNullOrWhiteSpace(requestedCompany))
                {
                    return BadRequest(new
                    {
                        success = false,

                        message = "COMPANY_ADMIN must have a Company."
                    });
                }

                if (!string.IsNullOrWhiteSpace(requestedDepartment))
                {
                    return BadRequest(new
                    {
                        success = false,

                        message = "COMPANY_ADMIN cannot have a Department."
                    });
                }
            }

            if (requestedRole == "USER")
            {
                if (string.IsNullOrWhiteSpace(requestedCompany))
                {
                    return BadRequest(new
                    {
                        success = false,

                        message =  "USER must have a Company."
                    });
                }

                if (string.IsNullOrWhiteSpace(requestedDepartment))
                {
                    return BadRequest(new
                    {
                        success = false,

                        message = "USER must have a Department."
                    });
                }
            }
            var exists =  await _authRepository.UsernameExistsAsync(username );

            if (exists)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Username already exists."
                });
            }

            var newUser = new DashboardUser
            {
                Username = username,

                Password = request.Password,

                FullName = string.IsNullOrWhiteSpace(request.FullName)
                        ? null
                        : request.FullName.Trim(),

                Role = requestedRole,

                Company = requestedCompany,

                Department = requestedDepartment,

                IsActive = 1
            };

            var userId =  await _authRepository.CreateUserAsync( newUser );

            return Ok(new
            {
                success = true,

                message = "User created successfully.",

                userId,

                username = newUser.Username,

                fullName =
                    newUser.FullName ?? "",

                role = newUser.Role,

                company = newUser.Company,

                department = newUser.Department
            });
        }
    }
}