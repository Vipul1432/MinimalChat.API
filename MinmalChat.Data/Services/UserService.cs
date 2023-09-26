using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MinmalChat.Data.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<MinimalChatUser> _userManager;
        private readonly IConfiguration _configuration;

        public UserService(UserManager<MinimalChatUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
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
            IdentityResult result;
            if(model.Password == null)
            {
                result = await _userManager.CreateAsync(user);
            }
            else
            {
                result = await _userManager.CreateAsync(user, model.Password);
            }

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

        /// <summary>
        /// Retrieves a user by their email address asynchronously.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <returns>The user object if found; otherwise, null.</returns>
        public async Task<MinimalChatUser?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        /// <summary>
        /// Validates a user's password asynchronously.
        /// </summary>
        /// <param name="user">The user whose password needs validation.</param>
        /// <param name="password">The password to validate.</param>
        /// <returns>True if the password is valid for the user; otherwise, false.</returns>
        public async Task<bool> ValidatePasswordAsync(MinimalChatUser user, string password)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

        /// <summary>
        /// Generates a JWT token for user authentication.
        /// </summary>
        /// <param name="user">The user for whom the token is generated.</param>
        /// <returns>The generated JWT token as a string.</returns>
        public string GenerateJwtToken(MinimalChatUser user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Name),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["JWT:ValidIssuer"],
                _configuration["JWT:ValidAudience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["JWT:LifetimeInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Retrieves a list of all registered users asynchronously.
        /// </summary>
        /// <returns>A Task containing a list of MinimalChatUser objects.</returns>
        /// <remarks>
        /// This method asynchronously fetches a list of all registered users in the system.
        /// </remarks>
        public async Task<List<MinimalChatUser>> GetAllUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        /// <summary>
        /// Checks if a user with the specified user ID exists in the database.
        /// </summary>
        /// <param name="userId">The ID of the user to check for existence.</param>
        /// <returns>True if the user exists; otherwise, false.</returns>
        public async Task<bool> GetUserByIdAsync(string userId)
        {
           var existUser =  await _userManager.Users.FirstOrDefaultAsync(user => user.Id == userId);
            if (existUser == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieves a user by their username asynchronously.
        /// </summary>
        /// <param name="username">The username (email address) of the user to retrieve.</param>
        /// <returns>The user object if found; otherwise, null.</returns>
        public async Task<MinimalChatUser?> GetUserByNameAsync(string username)
        {
            return await _userManager.FindByEmailAsync(username);
        }
    }
}
