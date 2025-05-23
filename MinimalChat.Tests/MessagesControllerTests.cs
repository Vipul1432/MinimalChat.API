using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MinimalChat.API.Controllers;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace MinimalChat.Tests
{
    public class MessagesControllerTests
    {
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IUserService> _mockUserService; // Not directly used by controller methods, but good practice if needed later
        private readonly Mock<ILogger<MessagesController>> _mockLogger;
        private readonly MessagesController _controller;

        public MessagesControllerTests()
        {
            _mockMessageService = new Mock<IMessageService>();
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<MessagesController>>();
            _controller = new MessagesController(_mockMessageService.Object, _mockUserService.Object, _mockLogger.Object);
        }

        private void SetupUserContext(string userId)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task SendMessage_ValidModel_ReturnsOk()
        {
            // Arrange
            var senderUserId = Guid.NewGuid().ToString();
            SetupUserContext(senderUserId);
            var messageDto = new MessageDto { ReceiverId = Guid.NewGuid().ToString(), Content = "Hello" };
            var expectedResponse = new ResponseMessageDto { Id = 1, SenderId = Guid.Parse(senderUserId), ReceiverId = Guid.Parse(messageDto.ReceiverId), Content = messageDto.Content };
            
            _mockMessageService.Setup(s => s.SendMessageAsync(It.Is<MessageDto>(m => m.SenderId == senderUserId && m.ReceiverId == messageDto.ReceiverId)))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendMessage(messageDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
            _mockMessageService.Verify(s => s.SendMessageAsync(It.Is<MessageDto>(m => m.SenderId == senderUserId)), Times.Once);
        }

        [Fact]
        public async Task SendMessage_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var senderUserId = Guid.NewGuid().ToString();
            SetupUserContext(senderUserId);
            _controller.ModelState.AddModelError("Content", "Content is required");
            var messageDto = new MessageDto { ReceiverId = Guid.NewGuid().ToString() }; // Invalid

            // Act
            var result = await _controller.SendMessage(messageDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _mockMessageService.Verify(s => s.SendMessageAsync(It.IsAny<MessageDto>()), Times.Never);
        }

        [Fact]
        public async Task SendMessage_SenderNotFound_ReturnsNotFound()
        {
            // Arrange
            var senderUserId = Guid.NewGuid().ToString();
            SetupUserContext(senderUserId);
            var messageDto = new MessageDto { ReceiverId = Guid.NewGuid().ToString(), Content = "Hello" };
            var exceptionMessage = $"Sender with ID {senderUserId} not found.";
            _mockMessageService.Setup(s => s.SendMessageAsync(It.Is<MessageDto>(m => m.SenderId == senderUserId)))
                .ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.SendMessage(messageDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public async Task SendMessage_ReceiverNotFound_ReturnsNotFound()
        {
            // Arrange
            var senderUserId = Guid.NewGuid().ToString();
            var receiverId = Guid.NewGuid().ToString();
            SetupUserContext(senderUserId);
            var messageDto = new MessageDto { ReceiverId = receiverId, Content = "Hello" };
            var exceptionMessage = $"Receiver with ID {receiverId} not found.";
            _mockMessageService.Setup(s => s.SendMessageAsync(It.Is<MessageDto>(m => m.SenderId == senderUserId)))
                .ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.SendMessage(messageDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public async Task GetConversationHistory_ValidUsers_ReturnsOk()
        {
            // Arrange
            var requestingUserId = Guid.NewGuid().ToString();
            var contactId = Guid.NewGuid();
            SetupUserContext(requestingUserId);
            var conversationDto = new ConversationHistoryDto { UserId1 = Guid.Parse(requestingUserId), UserId2 = contactId, Count = 10 };
            var expectedResponse = new List<ResponseMessageDto> { new ResponseMessageDto { Content = "Hi" } };
            _mockMessageService.Setup(s => s.GetConversationHistoryAsync(It.Is<ConversationHistoryDto>(d => d.UserId1 == Guid.Parse(requestingUserId) && d.UserId2 == contactId)))
                .ReturnsAsync(expectedResponse);
            
            // Act
            var result = await _controller.GetConversationHistory(contactId, null, 10, "asc");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
             _mockMessageService.Verify(s => s.GetConversationHistoryAsync(It.Is<ConversationHistoryDto>(
                d => d.UserId1 == Guid.Parse(requestingUserId) &&
                     d.UserId2 == contactId &&
                     d.Before == null &&
                     d.Count == 10 &&
                     d.SortOrder == "asc"
            )), Times.Once);
        }

        [Fact]
        public async Task GetConversationHistory_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var requestingUserId = Guid.NewGuid().ToString();
            var contactId = Guid.NewGuid();
            SetupUserContext(requestingUserId);
            var exceptionMessage = "User not found.";
             _mockMessageService.Setup(s => s.GetConversationHistoryAsync(It.Is<ConversationHistoryDto>(d => d.UserId1 == Guid.Parse(requestingUserId) && d.UserId2 == contactId)))
                .ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.GetConversationHistory(contactId, null, 10, "asc");

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public async Task EditMessage_ValidModel_ReturnsOk()
        {
            // Arrange
            var editorUserId = Guid.NewGuid().ToString();
            SetupUserContext(editorUserId);
            var messageId = 1;
            var editDto = new EditMessageDto { Content = "Updated content" };
            var expectedResponse = new ResponseMessageDto { Id = messageId, Content = editDto.Content, SenderId = Guid.Parse(editorUserId) };
            _mockMessageService.Setup(s => s.EditMessageAsync(messageId, editDto, editorUserId)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.EditMessage(messageId, editDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task EditMessage_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var editorUserId = Guid.NewGuid().ToString();
            SetupUserContext(editorUserId);
            _controller.ModelState.AddModelError("Content", "Content is required");
            var messageId = 1;
            var editDto = new EditMessageDto { }; // Invalid

            // Act
            var result = await _controller.EditMessage(messageId, editDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _mockMessageService.Verify(s => s.EditMessageAsync(It.IsAny<int>(), It.IsAny<EditMessageDto>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EditMessage_MessageNotFound_ReturnsNotFound()
        {
            // Arrange
            var editorUserId = Guid.NewGuid().ToString();
            SetupUserContext(editorUserId);
            var messageId = 1;
            var editDto = new EditMessageDto { Content = "Updated" };
            var exceptionMessage = "Message not found.";
            _mockMessageService.Setup(s => s.EditMessageAsync(messageId, editDto, editorUserId))
                .ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.EditMessage(messageId, editDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public async Task EditMessage_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var editorUserId = Guid.NewGuid().ToString();
            SetupUserContext(editorUserId);
            var messageId = 1;
            var editDto = new EditMessageDto { Content = "Updated" };
            var exceptionMessage = "Unauthorized to edit.";
            _mockMessageService.Setup(s => s.EditMessageAsync(messageId, editDto, editorUserId))
                .ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

            // Act
            var result = await _controller.EditMessage(messageId, editDto);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public async Task DeleteMessage_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var deleterUserId = Guid.NewGuid().ToString();
            SetupUserContext(deleterUserId);
            var messageId = 1;
            _mockMessageService.Setup(s => s.DeleteMessageAsync(messageId, deleterUserId)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteMessage(messageId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteMessage_MessageNotFound_ReturnsNotFound()
        {
            // Arrange
            var deleterUserId = Guid.NewGuid().ToString();
            SetupUserContext(deleterUserId);
            var messageId = 1;
            var exceptionMessage = "Message not found.";
            _mockMessageService.Setup(s => s.DeleteMessageAsync(messageId, deleterUserId))
                .ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.DeleteMessage(messageId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public async Task DeleteMessage_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var deleterUserId = Guid.NewGuid().ToString();
            SetupUserContext(deleterUserId);
            var messageId = 1;
            var exceptionMessage = "Unauthorized to delete.";
            _mockMessageService.Setup(s => s.DeleteMessageAsync(messageId, deleterUserId))
                .ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

            // Act
            var result = await _controller.DeleteMessage(messageId);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().Be(exceptionMessage);
        }
        
        [Fact]
        public async Task DeleteMessage_ServiceReturnsFalse_ReturnsBadRequest()
        {
            // Arrange
            var deleterUserId = Guid.NewGuid().ToString();
            SetupUserContext(deleterUserId);
            var messageId = 1;
            _mockMessageService.Setup(s => s.DeleteMessageAsync(messageId, deleterUserId)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteMessage(messageId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("Failed to delete message.");
        }

        [Fact]
        public async Task GetMessages_ReturnsOkWithMessages()
        {
            // Arrange
            var getMessagesDto = new GetMessagesDto { Query = "test", Page = 1, PageSize = 5, SortOrder = "asc" };
            var expectedResponse = new List<ResponseMessageDto> { new ResponseMessageDto { Content = "Test Message" } };
            _mockMessageService.Setup(s => s.GetMessagesAsync(getMessagesDto)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetMessages(getMessagesDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task GetMessages_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("PageSize", "PageSize must be positive.");
            var getMessagesDto = new GetMessagesDto { PageSize = -1 }; // Invalid

            // Act
            var result = await _controller.GetMessages(getMessagesDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _mockMessageService.Verify(s => s.GetMessagesAsync(It.IsAny<GetMessagesDto>()), Times.Never);
        }
    }
}
