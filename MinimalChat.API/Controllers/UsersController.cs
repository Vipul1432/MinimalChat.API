using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using System.Security.Claims;

namespace MinimalChat.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly AppSettings _applicationSettings;
        public UsersController(IUserService userService, IOptions<AppSettings> applicationSettings)
        {
            _userService = userService;
            _applicationSettings = applicationSettings.Value;
        }

        /// <summary>
        /// Registers a new user with the provided registration information.
        /// </summary>
        /// <param name="model">The registration data.</param>
        /// <returns>
        /// - 200 OK if registration is successful.
        /// - 400 Bad Request if the provided data is invalid.
        /// - 409 Conflict if the email is already registered.
        /// </returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Registration model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<RegistrationDto>
                {
                    Message = "Registration failed due to validation errors.",
                    Data = null,
                    StatusCode = 400
                });
            }

            var registrationResult = await _userService.RegisterAsync(model);

            return Ok(registrationResult);
        }

        /// <summary>
        /// Handles user login by validating credentials and generating a JWT token upon successful login.
        /// </summary>
        /// <param name="model">The login request model containing email and password.</param>
        /// <returns>
        /// - 200 OK with a JWT token and login success message if login is successful.
        /// - 400 Bad Request if validation errors occur during login.
        /// - 401 Unauthorized if login fails due to incorrect credentials.
        /// </returns>  
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<LoginDto>
                {
                    Message = "Login failed due to validation errors.",
                    Data = null,
                    StatusCode = 400
                });
            }

            var user = await _userService.GetUserByEmailAsync(model.Email);

            if (user == null)
            {
                return Unauthorized(new ApiResponse<LoginDto>
                {
                    Message = "Login failed due to incorrect credentials.",
                    Data = null,
                    StatusCode = 401
                });
            }

            if (!await _userService.ValidatePasswordAsync(user, model.Password))
            {
                return BadRequest(new ApiResponse<LoginDto>
                {
                    Message = "Login failed due to incorrect credentials.",
                    Data = null,
                    StatusCode = 401
                });
            }

            // Generate the JWT token
            var token = _userService.GenerateJwtToken(user);

            return Ok(new ApiResponse<LoginDto>
            {
                Message = "Login successful",
                Data = new LoginDto
                {
                    Email = model.Email,
                    JwtToken = token,
                },
                StatusCode = 200
            });
        }

        /// <summary>
        /// Logs in a user using Google credentials provided as a JSON Web Token (JWT).
        /// </summary>
        /// <param name="credential">The Google credential as a JWT string.</param>
        /// <returns>
        /// An IActionResult representing the login result, including a JWT token, message, and status code.
        /// </returns>
        /// <remarks>
        /// This action method validates the Google JWT credential, checks if the user exists,
        /// and either logs in the user or registers a new user if not found.
        /// </remarks>
        [HttpPost("googleLogin")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] string credential)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string> { this._applicationSettings.GoogleClientId }
            };

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);

                var user = await _userService.GetUserByNameAsync(payload.Email);

                if (user != null)
                {
                    var token = _userService.GenerateJwtToken(user);

                    return Ok(new ApiResponse<string>
                    {
                        Message = "Login successful",
                        Data = token,
                        StatusCode = 200 // Success
                    });
                }
                else
                {
                    Registration googleUser = new Registration()
                    {
                        Email = payload.Email,
                        Name = payload.Name,
                        Password = null
                    };
                    var registrationResult = await _userService.RegisterAsync(googleUser);
                    var registedUser = await _userService.GetUserByNameAsync(googleUser.Email);
                    var token = _userService.GenerateJwtToken(registedUser);
                    return Ok(new ApiResponse<string>
                    {
                        Message = "Login successful",
                        Data = token,
                        StatusCode = 200 // Success
                    });
                }
            }
            catch (Exception ex)
            {
                // Handle any exception that occurs during Google validation
                return StatusCode(500, new ApiResponse<string>
                {
                    Message = "Error validating Google credentials",
                    StatusCode = 500 // Internal Server Error
                });
            }
        }

        /// <summary>
        /// Retrieves a list of users, excluding the currently logged-in user.
        /// </summary>
        /// <returns>An IActionResult containing a list of UserDto objects.</returns>
        /// <remarks>
        /// This endpoint retrieves a list of all users registered in the system, excluding the
        /// currently authenticated user. It skips the current user while constructing the user list.
        /// </remarks>
        [HttpGet("users")]
        [Authorize]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                // fetch current UserId
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var users = await _userService.GetAllUsersAsync();

                var usersList = new List<UserDto>();

                foreach (var user in users)
                {
                    // Skip the current user
                    if (user.Id == currentUserId)
                        continue;
                    usersList.Add(new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email
                    });
                }

                return Ok(new ApiResponse<List<UserDto>>
                {
                    Message = "User list retrieved successfully",
                    Data = usersList,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Message = ex.Message,
                    Data = null,
                    StatusCode = 500
                });
            }
        }
    }
}
