using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Registers a new user asynchronously using the provided registration model.
        /// </summary>
        /// <param name="model">The registration data.</param>
        /// <returns>
        /// An ApiResponse containing the registration result, including a message and status code.
        /// </returns>
        Task<ApiResponse<RegistrationDto>> RegisterAsync(Registration model);

        /// <summary>
        /// Retrieves a user by their email address asynchronously.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <returns>The user object if found; otherwise, null.</returns>
        Task<MinimalChatUser?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Validates a user's password asynchronously.
        /// </summary>
        /// <param name="user">The user whose password needs validation.</param>
        /// <param name="password">The password to validate.</param>
        /// <returns>True if the password is valid for the user; otherwise, false.</returns>
        Task<bool> ValidatePasswordAsync(MinimalChatUser user, string password);

        /// <summary>
        /// Generates a JWT token for user authentication.
        /// </summary>
        /// <param name="user">The user for whom the token is generated.</param>
        /// <returns>The generated JWT token as a string.</returns>
        string GenerateJwtToken(MinimalChatUser user);

        /// <summary>
        /// Retrieves a list of all registered users asynchronously.
        /// </summary>
        /// <returns>A Task containing a list of MinimalChatUser objects.</returns>
        /// <remarks>
        /// This method asynchronously fetches a list of all registered users in the system.
        /// </remarks>
        Task<List<MinimalChatUser>> GetAllUsersAsync(bool isOnlyUserList, string currentUserId);

        /// <summary>
        /// Checks if a user with the specified user ID exists in the database.
        /// </summary>
        /// <param name="userId">The ID of the user to check for existence.</param>
        /// <returns>True if the user exists; otherwise, false.</returns>
        Task<bool> GetUserByIdAsync(string userId);

        /// <summary>
        /// Retrieves a user by their username asynchronously.
        /// </summary>
        /// <param name="username">The username (email address) of the user to retrieve.</param>
        /// <returns>The user object if found; otherwise, null.</returns>
        Task<MinimalChatUser?> GetUserByNameAsync(string username);

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
        Task<ApiResponse<LoginDto>> LoginAsync(Login model);

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
        Task<ApiResponse<LoginDto>> GoogleLoginAsync(string email, string name);

        /// <summary>
        /// Asynchronously generates a new access token and refresh token for a user based on their expired access token and refresh token.
        /// </summary>
        /// <param name="tokenModel">A model containing the expired access token and refresh token.</param>
        /// <returns>
        /// Returns an ApiResponse<TokenModel> representing the result of the token refresh operation,
        /// including a new access token and refresh token if successful, or an error message if unsuccessful.
        /// </returns>
        Task<ApiResponse<TokenModel>> GetRefreshTokenAsync(TokenModel tokenModel);

        Task<string> GetUserNameByIdAsync(string id);
    }
}
