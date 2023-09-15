using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public UsersController(IUserService userService)
        {
            _userService = userService;
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

            return StatusCode(registrationResult.StatusCode, registrationResult);
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
                return Unauthorized(new ApiResponse<LoginDto>
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
