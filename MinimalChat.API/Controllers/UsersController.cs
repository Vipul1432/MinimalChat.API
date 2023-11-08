using AutoMapper;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

namespace MinimalChat.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly AppSettings _applicationSettings;
        private readonly IMapper _mapper;

        public UsersController(IUserService userService, IOptions<AppSettings> applicationSettings, IMapper mapper)
        {
            _userService = userService;
            _applicationSettings = applicationSettings.Value;
            _mapper = mapper;
        }

        #region User Registration

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
        public async Task<IActionResult> RegisterAsync([FromBody] RegistrationDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<SocialRegistrationDto>
                    {
                        Message = "Registration failed due to validation errors.",
                        Data = null,
                        StatusCode = HttpStatusCode.BadRequest
                    });
                }

                var registrationResult = await _userService.RegisterAsync(model);

                return Ok(new ApiResponse<SocialRegistrationDto>
                {
                    Message = registrationResult.Message,
                    Data = registrationResult.Data,
                    StatusCode = registrationResult.StatusCode
                });
            }
            catch (Exception)
            {
                // Handle the exception, log it, and return an error response
                return StatusCode((int)HttpStatusCode.InternalServerError, new ApiResponse<object>
                {
                    Message = "An error occurred during registration! Try Again.",
                    Data = null,
                    StatusCode = HttpStatusCode.InternalServerError
                });
            }
        }

        #endregion User Registration

        #region User Login

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
        public async Task<IActionResult> LoginAsync([FromBody] LoginDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<LoginResponseDto>
                    {
                        Message = "Login failed due to validation errors.",
                        Data = null,
                        StatusCode = HttpStatusCode.BadRequest
                    });
                }

                var LoginResult = await _userService.LoginAsync(model);
                return StatusCode((int)LoginResult.StatusCode, new ApiResponse<LoginResponseDto>
                {
                    Message = LoginResult.Message,
                    Data = LoginResult.Data,
                    StatusCode = LoginResult.StatusCode
                });
            }
            catch (Exception)
            {
                return new ObjectResult(new ApiResponse<Object>
                {
                    Message = "An error occurred! Please try again.",
                    Data = null,
                    StatusCode = HttpStatusCode.InternalServerError
                });
            }
        }

        #endregion User Login

        #region Social Login(Google)

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
        public async Task<IActionResult> LoginWithGoogleAsync([FromBody] string credential)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string> { this._applicationSettings.GoogleClientId }
            };

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);

                var GoogleLoginResult = await _userService.GoogleLoginAsync(payload.Email, payload.Name);
                return StatusCode((int)GoogleLoginResult.StatusCode, new ApiResponse<LoginResponseDto>
                {
                    Message = GoogleLoginResult.Message,
                    Data = GoogleLoginResult.Data,
                    StatusCode = GoogleLoginResult.StatusCode
                });
            }
            catch (Exception)
            {
                // Handle any exception that occurs during Google validation
                return new ObjectResult(new ApiResponse<Object>
                {
                    Message = "An error occurred! Please try again.",
                    Data = null,
                    StatusCode = HttpStatusCode.InternalServerError
                });
            }
        }

        #endregion Social Login(Google)

        #region Retrieve User List

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
        public async Task<IActionResult> GetAllUsersWithGroupsAsync([FromQuery] bool isOnlyUserList)
        {
            try
            {
                // fetch current UserId
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var users = await _userService.GetAllUsersWithGroupsAsync(isOnlyUserList, currentUserId);

                //Map MinimalChatUser to UserDto
                var usersList = _mapper.Map<List<UserDto>>(users);

                return Ok(new ApiResponse<List<UserDto>>
                {
                    Message = "User list retrieved successfully",
                    Data = usersList,
                    StatusCode = HttpStatusCode.OK
                });
            }
            catch (Exception)
            {
                return new ObjectResult(new ApiResponse<Object>
                {
                    Message = "An error occurred! Please try again.",
                    Data = null,
                    StatusCode = HttpStatusCode.InternalServerError
                });
            }
        }

        #endregion Retrieve User List

        #region Get Refresh Json Web Token 

        /// <summary>
        /// Handles the HTTP POST request for refreshing an access token using a valid refresh token.
        /// </summary>
        /// <param name="tokenModel">A model containing the refresh token.</param>
        /// <returns>
        /// Returns an IActionResult representing the result of the token refresh operation,
        /// including a new access token if successful or an error message if unsuccessful.
        /// </returns>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshTokenAsync(TokenModel tokenModel)
        
        {
            if (tokenModel is null)
            {
                return BadRequest("Invalid client request");
            }

            var RefreshTokenResult = await _userService.GetRefreshTokenAsync(tokenModel);
            return StatusCode((int)RefreshTokenResult.StatusCode, new ApiResponse<TokenModel>
            {
                Message = RefreshTokenResult.Message,
                Data = RefreshTokenResult.Data,
                StatusCode = RefreshTokenResult.StatusCode
            });
        }

        #endregion Get Refresh Json Web Token
    }
}
