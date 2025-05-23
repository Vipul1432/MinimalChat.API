using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace MinimalChat.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<IRepository<User>> _mockUserRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IRepository<User>>();
            _mockMapper = new Mock<IMapper>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Mock IConfiguration for JWT settings
            var jwtSettings = new Mock<IConfigurationSection>();
            jwtSettings.Setup(s => s.Value).Returns("TestSecretKeyForMinimalChatApp"); // Replace with a test secret
            _mockConfiguration.Setup(c => c.GetSection("Jwt:SecretKey")).Returns(jwtSettings.Object);
            _mockConfiguration.Setup(c => c.GetSection("Jwt:Issuer")).Returns(new Mock<IConfigurationSection>().Object);
            _mockConfiguration.Setup(c => c.GetSection("Jwt:Audience")).Returns(new Mock<IConfigurationSection>().Object);


            _userService = new UserService(_mockUserRepository.Object, _mockMapper.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task RegisterUserAsync_Success()
        {
            // Arrange
            var registrationDto = new RegistrationDto { Name = "Test User", Email = "test@example.com", Password = "password" };
            var user = new User { Id = Guid.NewGuid(), Name = registrationDto.Name, Email = registrationDto.Email, PasswordHash = "hashedpassword" };
            var userDto = new UserDto { Id = user.Id, Name = user.Name, Email = user.Email };

            _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User)null); // Simulate no existing user
            _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
                .Returns(userDto);
            _mockMapper.Setup(m => m.Map<User>(It.IsAny<RegistrationDto>())).Returns(user);


            // Act
            var result = await _userService.RegisterUserAsync(registrationDto);

            // Assert
            _mockUserRepository.Verify(repo => repo.AddAsync(It.Is<User>(u => u.Email == registrationDto.Email && !string.IsNullOrEmpty(u.PasswordHash))), Times.Once);
            result.Should().NotBeNull();
            result.Email.Should().Be(registrationDto.Email);
            result.Name.Should().Be(registrationDto.Name);
        }

        [Fact]
        public async Task RegisterUserAsync_UserAlreadyExists()
        {
            // Arrange
            var registrationDto = new RegistrationDto { Email = "existing@example.com", Password = "password" };
            var existingUser = new User { Email = registrationDto.Email };

            _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(existingUser); // Simulate existing user

            // Act
            Func<Task> act = async () => await _userService.RegisterUserAsync(registrationDto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("User with this email already exists.");
            _mockUserRepository.Verify(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
            _mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_Success()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "password" };
            var user = new User { Id = Guid.NewGuid(), Email = loginDto.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginDto.Password) };
            var userDto = new UserDto { Id = user.Id, Email = user.Email, Name = "Test User" };

            _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);
            _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

            // Act
            var result = await _userService.LoginAsync(loginDto);

            // Assert
            _mockUserRepository.Verify(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
            result.Should().NotBeNull();
            result.Token.Should().NotBeEmpty();
            result.User.Should().NotBeNull();
            result.User.Email.Should().Be(loginDto.Email);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "password" };

            _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User)null); // Simulate user not found

            // Act
            Func<Task> act = async () => await _userService.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("User not found.");
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "wrongpassword" };
            var user = new User { Email = loginDto.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword") };

            _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () => await _userService.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid credentials.");
        }

        [Fact]
        public async Task GetUserByIdAsync_UserFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "Test User", Email = "test@example.com" };
            var userDto = new UserDto { Id = userId, Name = "Test User", Email = "test@example.com" };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
            result.Should().NotBeNull();
            result.Id.Should().Be(userId);
        }

        [Fact]
        public async Task GetUserByIdAsync_UserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act
            Func<Task> act = async () => await _userService.GetUserByIdAsync(userId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"User with ID {userId} not found.");
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetAllUsersAsync_ReturnsAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Name = "User 1", Email = "user1@example.com" },
                new User { Id = Guid.NewGuid(), Name = "User 2", Email = "user2@example.com" }
            };
            var userDtos = users.Select(u => new UserDto { Id = u.Id, Name = u.Name, Email = u.Email }).ToList();

            _mockUserRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(users);
            _mockMapper.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(userDtos);

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            _mockUserRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
            result.Should().NotBeNull();
            result.Should().HaveCount(users.Count);
            result.Should().BeEquivalentTo(userDtos);
        }

        [Fact]
        public async Task LoginWithGoogleAsync_NewUser()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto { GoogleId = "google123", Email = "newgoogleuser@example.com", Name = "Google User" };
            var newUser = new User { Id = Guid.NewGuid(), GoogleId = googleLoginDto.GoogleId, Email = googleLoginDto.Email, Name = googleLoginDto.Name };
            var userDto = new UserDto { Id = newUser.Id, GoogleId = newUser.GoogleId, Email = newUser.Email, Name = newUser.Name };

            _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User)null); // Simulate no existing user
            _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<User>(googleLoginDto)).Returns(newUser); // Map DTO to User for saving
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(userDto);


            // Act
            var result = await _userService.LoginWithGoogleAsync(googleLoginDto);

            // Assert
            _mockUserRepository.Verify(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
            _mockUserRepository.Verify(repo => repo.AddAsync(It.Is<User>(u => u.GoogleId == googleLoginDto.GoogleId)), Times.Once);
            result.Should().NotBeNull();
            result.Token.Should().NotBeEmpty();
            result.User.Should().NotBeNull();
            result.User.Email.Should().Be(googleLoginDto.Email);
            result.User.Name.Should().Be(googleLoginDto.Name);
        }

        [Fact]
        public async Task LoginWithGoogleAsync_ExistingUser()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto { GoogleId = "google123", Email = "existinggoogleuser@example.com", Name = "Google User" };
            var existingUser = new User { Id = Guid.NewGuid(), GoogleId = googleLoginDto.GoogleId, Email = googleLoginDto.Email, Name = "Old Name" }; // Name might be different
            var userDto = new UserDto { Id = existingUser.Id, GoogleId = existingUser.GoogleId, Email = existingUser.Email, Name = existingUser.Name };


            _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(existingUser);
            _mockMapper.Setup(m => m.Map<UserDto>(existingUser)).Returns(userDto);


            // Act
            var result = await _userService.LoginWithGoogleAsync(googleLoginDto);

            // Assert
            _mockUserRepository.Verify(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
            _mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
            result.Should().NotBeNull();
            result.Token.Should().NotBeEmpty();
            result.User.Should().NotBeNull();
            result.User.Email.Should().Be(existingUser.Email); // Email from existing user
            result.User.Name.Should().Be(existingUser.Name);   // Name from existing user
        }
    }
}
