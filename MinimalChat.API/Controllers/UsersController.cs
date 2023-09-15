using Microsoft.AspNetCore.Mvc;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;

namespace MinimalChat.API.Controllers
{
    [Route("api/[controller]")]
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
    }
}
