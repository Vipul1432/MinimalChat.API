using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Context;
using MinmalChat.Data.Helpers;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MinmalChat.Data.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<MinimalChatUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly MinimalChatDbContext _context;

        public UserService(UserManager<MinimalChatUser> userManager, IConfiguration configuration, MinimalChatDbContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
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
        /// Asynchronously validates a user's login credentials, generates JWT and refresh tokens upon successful login,
        /// and returns an ApiResponse containing user login information or an error message if login fails.
        /// </summary>
        /// <param name="model">A Login object containing user credentials (email and password).</param>
        /// <returns>
        /// Returns an ApiResponse<LoginDto> representing the result of the login operation,
        /// including user login information (JWT token, refresh token) if successful,
        /// or an error message if login fails due to incorrect credentials.
        /// </returns>
        public async Task<ApiResponse<LoginDto>> LoginAsync(Login model)
        {
            var user = await _userManager.FindByNameAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);
                user.RefreshTokenExpiryTime = DateTime.Now.AddDays(refreshTokenValidityInDays);
                await _userManager.UpdateAsync(user);
                return new ApiResponse<LoginDto>
                {
                    Message = "Login successfully!",
                    Data = new LoginDto
                    {
                        Email = model.Email,
                        JwtToken = token,
                        RefreshToken = refreshToken,
                    },
                    StatusCode = 200
                };
            }
            else
            {
                return new ApiResponse<LoginDto>
                {
                    Message = "Login failed due to incorrect credentials.",
                    Data = null,
                    StatusCode = 401
                };
            }
        }

        /// <summary>
        /// Asynchronously handles user login via Google authentication, generates JWT and refresh tokens upon successful login,
        /// and returns an ApiResponse containing user login information or an error message if login fails.
        /// </summary>
        /// <param name="email">The user's email obtained from Google authentication.</param>
        /// <param name="name">The user's name obtained from Google authentication.</param>
        /// <returns>
        /// Returns an ApiResponse<LoginDto> representing the result of the Google login operation,
        /// including user login information (JWT token, refresh token) if successful,
        /// or an error message if login fails.
        /// </returns>
        public async Task<ApiResponse<LoginDto>> GoogleLoginAsync(string email, string name)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);
                user.RefreshTokenExpiryTime = DateTime.Now.AddDays(refreshTokenValidityInDays);
                await _userManager.UpdateAsync(user);
                return new ApiResponse<LoginDto>
                {
                    Message = "Login successfully!",
                    Data = new LoginDto
                    {
                        Email = email,
                        JwtToken = token,
                        RefreshToken = refreshToken,
                    },
                    StatusCode = 200
                };
            }
            else
            {
                Registration googleUser = new Registration()
                {
                    Email = email,
                    Name = name,
                    Password = null
                };
                await RegisterAsync(googleUser);
                var registedUser = await _userManager.FindByEmailAsync(googleUser.Email);
                var token = GenerateJwtToken(registedUser);
                var refreshToken = GenerateRefreshToken();
                registedUser.RefreshToken = refreshToken;
                _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);
                registedUser.RefreshTokenExpiryTime = DateTime.Now.AddDays(refreshTokenValidityInDays);
                await _userManager.UpdateAsync(registedUser);
                return new ApiResponse<LoginDto>
                {
                    Message = "Login successfully!",
                    Data = new LoginDto
                    {
                        Email = email,
                        JwtToken = token,
                        RefreshToken = refreshToken,
                    },
                    StatusCode = 200
                };
            }
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["JWT:ValidIssuer"],
                _configuration["JWT:ValidAudience"],
                claims,
                expires: DateTime.Now.AddDays(Convert.ToInt32(_configuration["JWT:LifetimeInDays"])),
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
        public async Task<List<MinimalChatUser>> GetAllUsersAsync(bool isOnlyUserList, string currentUserId)
        {
            var users = await _userManager.Users.ToListAsync();
            if (!isOnlyUserList)
            {
                var userGroups = await _context.Groups.Include(x => x.Members).Where(g => g.Members!.Any(m => m.UserId == currentUserId)).ToListAsync();
                var usersFromGroups = userGroups.Select(group => new MinimalChatUser
                {
                    Id = group.Id.ToString(),
                    Name = group.Name,
                }).ToList();

                // Append the users from groups to the original list of users
                users.AddRange(usersFromGroups);
            }
            return users;
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

        /// <summary>
        /// Asynchronously generates a new access token and refresh token for a user based on their expired access token and refresh token.
        /// </summary>
        /// <param name="tokenModel">A model containing the expired access token and refresh token.</param>
        /// <returns>
        /// Returns an ApiResponse<TokenModel> representing the result of the token refresh operation,
        /// including a new access token and refresh token if successful, or an error message if unsuccessful.
        /// </returns>
        public async Task<ApiResponse<TokenModel>> GetRefreshTokenAsync(TokenModel tokenModel)
        {
            string? accessToken = tokenModel.AccessToken;
            string? refreshToken = tokenModel.RefreshToken;

            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                return new ApiResponse<TokenModel>
                {
                    Message = "Invalid access token or refresh token.",
                    Data = null,
                    StatusCode = 401
                };
            }
            var username = principal.FindFirst(ClaimTypes.NameIdentifier)!;

            var user = await _userManager.Users.FirstOrDefaultAsync(user => user.Id == username.Value);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return new ApiResponse<TokenModel>
                {
                    Message = "Invalid access token or refresh token.",
                    Data = null,
                    StatusCode = 401
                };
            }

            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userManager.UpdateAsync(user);
            return new ApiResponse<TokenModel>
            {
                Message = "Refresh token generate successfully!",
                Data = new TokenModel
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                },
                StatusCode = 200
            };
        }

        /// <summary>
        /// Generates a random refresh token for user authentication.
        /// </summary>
        /// <returns>A securely generated refresh token as a Base64-encoded string.</returns>
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Validates and extracts a ClaimsPrincipal from an expired JWT token.
        /// </summary>
        /// <param name="token">The expired JWT token to validate and parse.</param>
        /// <returns>
        /// A ClaimsPrincipal representing the user claims if the token is valid,
        /// or null if the token is not valid or cannot be parsed.
        /// </returns>
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;

        }

        public async Task<string> GetUserNameByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                return user.Name;
            }
            return null;
        }
    }
}
