using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MinimalChat.API.Controllers;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MinimalChat.Tests
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<UsersController>>(); // Or NullLogger<UsersController>.Instance
            _controller = new UsersController(_mockUserService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Register_ValidModel_ReturnsOk()
        {
            // Arrange
            var registrationDto = new RegistrationDto { Name = "Test User", Email = "test@example.com", Password = "Password123!" };
            var userDto = new UserDto { Id = Guid.NewGuid(), Name = registrationDto.Name, Email = registrationDto.Email };
            _mockUserService.Setup(s => s.RegisterUserAsync(registrationDto)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.Register(registrationDto);

            // Assert
            _mockUserService.Verify(s => s.RegisterUserAsync(registrationDto), Times.Once);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(userDto);
        }

        [Fact]
        public async Task Register_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Email", "Email is required");
            var registrationDto = new RegistrationDto { Name = "Test User", Password = "Password123!" }; // Invalid DTO

            // Act
            var result = await _controller.Register(registrationDto);

            // Assert
            _mockUserService.Verify(s => s.RegisterUserAsync(It.IsAny<RegistrationDto>()), Times.Never);
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Register_UserServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var registrationDto = new RegistrationDto { Name = "Test User", Email = "test@example.com", Password = "Password123!" };
            var exceptionMessage = "Email already exists.";
            _mockUserService.Setup(s => s.RegisterUserAsync(registrationDto)).ThrowsAsync(new ArgumentException(exceptionMessage));

            // Act
            var result = await _controller.Register(registrationDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Password123!" };
            var loginResponseDto = new LoginResponseDto { Token = "test_token", User = new UserDto { Email = loginDto.Email } };
            _mockUserService.Setup(s => s.LoginAsync(loginDto)).ReturnsAsync(loginResponseDto);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            _mockUserService.Verify(s => s.LoginAsync(loginDto), Times.Once);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(loginResponseDto);
        }

        [Fact]
        public async Task Login_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Email", "Email is required");
            var loginDto = new LoginDto { Password = "Password123!" };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            _mockUserService.Verify(s => s.LoginAsync(It.IsAny<LoginDto>()), Times.Never);
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Login_UserServiceThrowsKeyNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "Password123!" };
            var exceptionMessage = "User not found.";
            _mockUserService.Setup(s => s.LoginAsync(loginDto)).ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public async Task Login_UserServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "WrongPassword" };
            var exceptionMessage = "Invalid credentials.";
            _mockUserService.Setup(s => s.LoginAsync(loginDto)).ThrowsAsync(new ArgumentException(exceptionMessage));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public async Task GetUser_UserExists_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDto = new UserDto { Id = userId, Name = "Test User", Email = "test@example.com" };
            _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetUser(userId);

            // Assert
            _mockUserService.Verify(s => s.GetUserByIdAsync(userId), Times.Once);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(userDto);
        }

        [Fact]
        public async Task GetUser_UserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var exceptionMessage = $"User with ID {userId} not found.";
            _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.GetUser(userId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsOkWithUserList()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), Name = "User 1", Email = "user1@example.com" },
                new UserDto { Id = Guid.NewGuid(), Name = "User 2", Email = "user2@example.com" }
            };
            _mockUserService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            _mockUserService.Verify(s => s.GetAllUsersAsync(), Times.Once);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(users);
        }

        [Fact]
        public async Task LoginWithGoogle_ValidToken_ReturnsOk()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto { GoogleId = "google123", Email = "googleuser@example.com", Name = "Google User" };
            var loginResponseDto = new LoginResponseDto { Token = "google_token", User = new UserDto { Email = googleLoginDto.Email, Name = googleLoginDto.Name } };
            _mockUserService.Setup(s => s.LoginWithGoogleAsync(googleLoginDto)).ReturnsAsync(loginResponseDto);

            // Act
            var result = await _controller.LoginWithGoogle(googleLoginDto);

            // Assert
            _mockUserService.Verify(s => s.LoginWithGoogleAsync(googleLoginDto), Times.Once);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(loginResponseDto);
        }

        [Fact]
        public async Task LoginWithGoogle_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("GoogleId", "GoogleId is required");
            var googleLoginDto = new GoogleLoginDto { Email = "googleuser@example.com", Name = "Google User" }; // Invalid

            // Act
            var result = await _controller.LoginWithGoogle(googleLoginDto);

            // Assert
            _mockUserService.Verify(s => s.LoginWithGoogleAsync(It.IsAny<GoogleLoginDto>()), Times.Never);
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task LoginWithGoogle_UserServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto { GoogleId = "google123", Email = "googleuser@example.com", Name = "Google User" };
            var exceptionMessage = "An error occurred during Google login.";
            _mockUserService.Setup(s => s.LoginWithGoogleAsync(googleLoginDto)).ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.LoginWithGoogle(googleLoginDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be(exceptionMessage);
        }
    }
}
