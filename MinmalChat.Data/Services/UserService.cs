using Microsoft.AspNetCore.Identity;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;

namespace MinmalChat.Data.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<MinimalChatUser> _userManager;

        public UserService(UserManager<MinimalChatUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Registers a new user asynchronously using the provided registration model.
        /// </summary>
        /// <param name="model">The registration data.</param>
        /// <returns>
        /// An ApiResponse containing the registration result, including a message and status code.
        /// </returns>
        public async Task<ApiResponse<RegistrationDto>> RegisterAsync(Registration model)
        {
            // Check if a user with the same email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return new ApiResponse<RegistrationDto>
                {
                    Message = "Registration failed. Email is already registered.",
                    Data = null,
                    StatusCode = 409 // Conflict
                };
            }

            var user = new MinimalChatUser
            {
                Name = model.Name,
                UserName = model.Email,
                Email = model.Email,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return new ApiResponse<RegistrationDto>
                {
                    Message = "Registration successful",
                    Data = new RegistrationDto
                    {
                        Name = model.Name,
                        Email = user.Email
                    },
                    StatusCode = 200 // Success
                };
            }

            return new ApiResponse<RegistrationDto>
            {
                Message = "Registration failed due to validation errors.",
                Data = null,
                StatusCode = 400 // BadRequest
            };
        }
    }
}
