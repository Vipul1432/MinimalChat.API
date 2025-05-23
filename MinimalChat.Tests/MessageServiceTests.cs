using AutoMapper;
using FluentAssertions;
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
    public class MessageServiceTests
    {
        private readonly Mock<IRepository<Message>> _mockMessageRepository;
        private readonly Mock<IRepository<User>> _mockUserRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly MessageService _messageService;

        public MessageServiceTests()
        {
            _mockMessageRepository = new Mock<IRepository<Message>>();
            _mockUserRepository = new Mock<IRepository<User>>();
            _mockMapper = new Mock<IMapper>();
            _messageService = new MessageService(_mockMessageRepository.Object, _mockUserRepository.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task SendMessageAsync_Success()
        {
            // Arrange
            var messageDto = new MessageDto { SenderId = Guid.NewGuid().ToString(), ReceiverId = Guid.NewGuid().ToString(), Content = "Hello!" };
            var sender = new User { Id = Guid.Parse(messageDto.SenderId), Name = "Sender" };
            var receiver = new User { Id = Guid.Parse(messageDto.ReceiverId), Name = "Receiver" };
            var message = new Message { Id = 1, SenderId = sender.Id, ReceiverId = receiver.Id, Content = messageDto.Content, Timestamp = DateTime.UtcNow };
            var responseMessageDto = new ResponseMessageDto { Id = message.Id, SenderId = message.SenderId, ReceiverId = message.ReceiverId, Content = message.Content, Timestamp = message.Timestamp, SenderName = sender.Name, ReceiverName = receiver.Name };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(sender.Id)).ReturnsAsync(sender);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(receiver.Id)).ReturnsAsync(receiver);
            _mockMapper.Setup(m => m.Map<Message>(messageDto)).Returns(message);
            _mockMessageRepository.Setup(repo => repo.AddAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<ResponseMessageDto>(It.IsAny<Message>())).Returns(responseMessageDto);
            _mockMapper.Setup(m => m.Map(It.IsAny<User>(), It.IsAny<ResponseMessageDto>()))
                .Callback<User, ResponseMessageDto>((src, dest) => {
                    if (src.Id == sender.Id) dest.SenderName = src.Name;
                    if (src.Id == receiver.Id) dest.ReceiverName = src.Name;
                });


            // Act
            var result = await _messageService.SendMessageAsync(messageDto);

            // Assert
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(sender.Id), Times.Once);
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(receiver.Id), Times.Once);
            _mockMessageRepository.Verify(repo => repo.AddAsync(It.Is<Message>(m => m.Content == messageDto.Content)), Times.Once);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(responseMessageDto);
            result.SenderName.Should().Be(sender.Name);
            result.ReceiverName.Should().Be(receiver.Name);
        }

        [Fact]
        public async Task SendMessageAsync_SenderNotFound()
        {
            // Arrange
            var messageDto = new MessageDto { SenderId = Guid.NewGuid().ToString(), ReceiverId = Guid.NewGuid().ToString(), Content = "Hello!" };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(Guid.Parse(messageDto.SenderId))).ReturnsAsync((User)null);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(Guid.Parse(messageDto.ReceiverId))).ReturnsAsync(new User());


            // Act
            Func<Task> act = async () => await _messageService.SendMessageAsync(messageDto);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Sender with ID {messageDto.SenderId} not found.");
        }

        [Fact]
        public async Task SendMessageAsync_ReceiverNotFound()
        {
            // Arrange
            var messageDto = new MessageDto { SenderId = Guid.NewGuid().ToString(), ReceiverId = Guid.NewGuid().ToString(), Content = "Hello!" };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(Guid.Parse(messageDto.SenderId))).ReturnsAsync(new User());
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(Guid.Parse(messageDto.ReceiverId))).ReturnsAsync((User)null);

            // Act
            Func<Task> act = async () => await _messageService.SendMessageAsync(messageDto);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Receiver with ID {messageDto.ReceiverId} not found.");
        }

        [Fact]
        public async Task GetConversationHistoryAsync_ValidUsers_ReturnsHistory()
        {
            // Arrange
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();
            var conversationDto = new ConversationHistoryDto { UserId1 = user1Id, UserId2 = user2Id, Count = 5, SortOrder = "asc" };
            var user1 = new User { Id = user1Id };
            var user2 = new User { Id = user2Id };

            var messages = new List<Message>
            {
                new Message { Id = 1, SenderId = user1Id, ReceiverId = user2Id, Content = "Msg1", Timestamp = DateTime.UtcNow.AddMinutes(-5) },
                new Message { Id = 2, SenderId = user2Id, ReceiverId = user1Id, Content = "Msg2", Timestamp = DateTime.UtcNow.AddMinutes(-4) },
                new Message { Id = 3, SenderId = user1Id, ReceiverId = user2Id, Content = "Msg3", Timestamp = DateTime.UtcNow.AddMinutes(-3) },
                new Message { Id = 4, SenderId = user2Id, ReceiverId = user1Id, Content = "Msg4", Timestamp = DateTime.UtcNow.AddMinutes(-2) },
                new Message { Id = 5, SenderId = user1Id, ReceiverId = user2Id, Content = "Msg5", Timestamp = DateTime.UtcNow.AddMinutes(-1) },
                new Message { Id = 6, SenderId = user1Id, ReceiverId = user2Id, Content = "Msg6-Older", Timestamp = DateTime.UtcNow.AddMinutes(-10) }, // Should be filtered by count if 'Before' not used
            };
            var responseMessageDtos = messages.Where(m=> m.Id <=5).Select(m => new ResponseMessageDto { Id = m.Id, Content = m.Content, Timestamp = m.Timestamp, SenderId = m.SenderId, ReceiverId = m.ReceiverId }).ToList();

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(user1Id)).ReturnsAsync(user1);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(user2Id)).ReturnsAsync(user2);
            // Let the service logic handle the filtering and sorting
            _mockMessageRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Message, bool>>>()))
                                 .ReturnsAsync((Expression<Func<Message, bool>> predicate) => messages.Where(predicate.Compile()).ToList());
            _mockMapper.Setup(m => m.Map<IEnumerable<ResponseMessageDto>>(It.IsAny<IEnumerable<Message>>())).Returns(responseMessageDtos);


            // Act
            var result = await _messageService.GetConversationHistoryAsync(conversationDto);

            // Assert
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(user1Id), Times.Once);
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(user2Id), Times.Once);
            _mockMessageRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Message, bool>>>()), Times.Once);
            result.Should().NotBeNull();
            result.Should().HaveCount(conversationDto.Count);
            result.Should().BeInAscendingOrder(r => r.Timestamp); // due to sortOrder = "asc"
            result.All(r => (r.SenderId == user1Id && r.ReceiverId == user2Id) || (r.SenderId == user2Id && r.ReceiverId == user1Id)).Should().BeTrue();
        }
        
        [Fact]
        public async Task GetConversationHistoryAsync_WithBefore_ReturnsCorrectHistory()
        {
            // Arrange
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();
            var beforeTimestamp = DateTime.UtcNow.AddMinutes(-2.5); // Before Msg3
            var conversationDto = new ConversationHistoryDto { UserId1 = user1Id, UserId2 = user2Id, Before = beforeTimestamp, Count = 2, SortOrder = "desc" };
            var user1 = new User { Id = user1Id };
            var user2 = new User { Id = user2Id };

            var messages = new List<Message>
            {
                new Message { Id = 1, SenderId = user1Id, ReceiverId = user2Id, Content = "Msg1", Timestamp = DateTime.UtcNow.AddMinutes(-5) }, // Should be included
                new Message { Id = 2, SenderId = user2Id, ReceiverId = user1Id, Content = "Msg2", Timestamp = DateTime.UtcNow.AddMinutes(-4) }, // Should be included
                new Message { Id = 3, SenderId = user1Id, ReceiverId = user2Id, Content = "Msg3", Timestamp = DateTime.UtcNow.AddMinutes(-3) }, // Should be included
                new Message { Id = 4, SenderId = user2Id, ReceiverId = user1Id, Content = "Msg4", Timestamp = DateTime.UtcNow.AddMinutes(-2) }, // After 'Before'
                new Message { Id = 5, SenderId = user1Id, ReceiverId = user2Id, Content = "Msg5", Timestamp = DateTime.UtcNow.AddMinutes(-1) }, // After 'Before'
            };
            // Expected: Msg3, Msg2 (Count = 2, Descending, Before Msg3's timestamp which is -3)
            var expectedMessages = new List<Message> { messages[2], messages[1] };
             var responseMessageDtos = expectedMessages.Select(m => new ResponseMessageDto { Id = m.Id, Content = m.Content, Timestamp = m.Timestamp, SenderId = m.SenderId, ReceiverId = m.ReceiverId }).ToList();


            _mockUserRepository.Setup(repo => repo.GetByIdAsync(user1Id)).ReturnsAsync(user1);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(user2Id)).ReturnsAsync(user2);
            _mockMessageRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Message, bool>>>()))
                                 .ReturnsAsync((Expression<Func<Message, bool>> predicate) => messages.Where(predicate.Compile()).ToList());
            _mockMapper.Setup(m => m.Map<IEnumerable<ResponseMessageDto>>(It.IsAny<IEnumerable<Message>>())).Returns(responseMessageDtos);


            // Act
            var result = await _messageService.GetConversationHistoryAsync(conversationDto);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(conversationDto.Count);
            result.Should().BeInDescendingOrder(r => r.Timestamp);
            result.Select(r => r.Id).Should().ContainInOrder(3, 2); // Msg3, then Msg2
            result.All(r => r.Timestamp < beforeTimestamp).Should().BeTrue();
        }

        [Fact]
        public async Task GetConversationHistoryAsync_User1NotFound()
        {
            // Arrange
            var conversationDto = new ConversationHistoryDto { UserId1 = Guid.NewGuid(), UserId2 = Guid.NewGuid() };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(conversationDto.UserId1)).ReturnsAsync((User)null);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(conversationDto.UserId2)).ReturnsAsync(new User());


            // Act
            Func<Task> act = async () => await _messageService.GetConversationHistoryAsync(conversationDto);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"User with ID {conversationDto.UserId1} not found.");
        }

        [Fact]
        public async Task GetConversationHistoryAsync_User2NotFound()
        {
            // Arrange
            var conversationDto = new ConversationHistoryDto { UserId1 = Guid.NewGuid(), UserId2 = Guid.NewGuid() };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(conversationDto.UserId1)).ReturnsAsync(new User());
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(conversationDto.UserId2)).ReturnsAsync((User)null);

            // Act
            Func<Task> act = async () => await _messageService.GetConversationHistoryAsync(conversationDto);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"User with ID {conversationDto.UserId2} not found.");
        }

        [Fact]
        public async Task GetConversationHistoryAsync_NoMessages_ReturnsEmpty()
        {
            // Arrange
            var conversationDto = new ConversationHistoryDto { UserId1 = Guid.NewGuid(), UserId2 = Guid.NewGuid(), Count = 10 };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(conversationDto.UserId1)).ReturnsAsync(new User());
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(conversationDto.UserId2)).ReturnsAsync(new User());
            _mockMessageRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Message, bool>>>())).ReturnsAsync(new List<Message>());
            _mockMapper.Setup(m => m.Map<IEnumerable<ResponseMessageDto>>(It.IsAny<IEnumerable<Message>>())).Returns(new List<ResponseMessageDto>());

            // Act
            var result = await _messageService.GetConversationHistoryAsync(conversationDto);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task EditMessageAsync_Success()
        {
            // Arrange
            var messageId = 1;
            var currentUserId = Guid.NewGuid().ToString();
            var editMessageDto = new EditMessageDto { Content = "Updated content" };
            var message = new Message { Id = messageId, SenderId = Guid.Parse(currentUserId), Content = "Original content", Timestamp = DateTime.UtcNow };
            var updatedMessage = new Message { Id = messageId, SenderId = Guid.Parse(currentUserId), Content = editMessageDto.Content, Timestamp = message.Timestamp }; // Timestamp shouldn't change
            var responseMessageDto = new ResponseMessageDto { Id = messageId, SenderId = Guid.Parse(currentUserId), Content = editMessageDto.Content, Timestamp = message.Timestamp };

            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync(message);
            _mockMessageRepository.Setup(repo => repo.UpdateAsync(It.Is<Message>(m => m.Id == messageId && m.Content == editMessageDto.Content))).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<ResponseMessageDto>(It.Is<Message>(m => m.Content == editMessageDto.Content))).Returns(responseMessageDto);
             _mockMapper.Setup(m => m.Map(editMessageDto, message)).Callback<EditMessageDto, Message>((src, dest) => {
                dest.Content = src.Content;
            });


            // Act
            var result = await _messageService.EditMessageAsync(messageId, editMessageDto, currentUserId);

            // Assert
            _mockMessageRepository.Verify(repo => repo.GetByIdAsync(messageId), Times.Once);
            _mockMessageRepository.Verify(repo => repo.UpdateAsync(It.Is<Message>(m => m.Content == editMessageDto.Content)), Times.Once);
            result.Should().NotBeNull();
            result.Content.Should().Be(editMessageDto.Content);
        }

        [Fact]
        public async Task EditMessageAsync_MessageNotFound()
        {
            // Arrange
            var messageId = 1;
            var editMessageDto = new EditMessageDto { Content = "Updated content" };
            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync((Message)null);

            // Act
            Func<Task> act = async () => await _messageService.EditMessageAsync(messageId, editMessageDto, Guid.NewGuid().ToString());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Message with ID {messageId} not found.");
        }

        [Fact]
        public async Task EditMessageAsync_UnauthorizedEdit()
        {
            // Arrange
            var messageId = 1;
            var ownerUserId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString(); // Different user
            var editMessageDto = new EditMessageDto { Content = "Updated content" };
            var message = new Message { Id = messageId, SenderId = ownerUserId, Content = "Original content" };

            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync(message);

            // Act
            Func<Task> act = async () => await _messageService.EditMessageAsync(messageId, editMessageDto, currentUserId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You are not authorized to edit this message.");
        }

        [Fact]
        public async Task DeleteMessageAsync_Success()
        {
            // Arrange
            var messageId = 1;
            var currentUserId = Guid.NewGuid().ToString();
            var message = new Message { Id = messageId, SenderId = Guid.Parse(currentUserId) };

            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync(message);
            _mockMessageRepository.Setup(repo => repo.DeleteAsync(message)).Returns(Task.CompletedTask);

            // Act
            var result = await _messageService.DeleteMessageAsync(messageId, currentUserId);

            // Assert
            _mockMessageRepository.Verify(repo => repo.GetByIdAsync(messageId), Times.Once);
            _mockMessageRepository.Verify(repo => repo.DeleteAsync(message), Times.Once);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteMessageAsync_MessageNotFound()
        {
            // Arrange
            var messageId = 1;
            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync((Message)null);

            // Act
            Func<Task> act = async () => await _messageService.DeleteMessageAsync(messageId, Guid.NewGuid().ToString());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Message with ID {messageId} not found.");
        }

        [Fact]
        public async Task DeleteMessageAsync_UnauthorizedDelete()
        {
            // Arrange
            var messageId = 1;
            var ownerUserId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString(); // Different user
            var message = new Message { Id = messageId, SenderId = ownerUserId };

            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync(message);

            // Act
            Func<Task> act = async () => await _messageService.DeleteMessageAsync(messageId, currentUserId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You are not authorized to delete this message.");
        }
        
        [Fact]
        public async Task GetMessagesAsync_DefaultParameters_ReturnsMessages()
        {
            // Arrange
            var getMessagesDto = new GetMessagesDto(); // Defaults: Page = 0, PageSize = 20, SortOrder = "desc"
            var messages = new List<Message>();
            for (int i = 1; i <= 25; i++) // More than default PageSize
            {
                messages.Add(new Message { Id = i, Content = $"Message {i}", Timestamp = DateTime.UtcNow.AddMinutes(-i) });
            }
            var expectedMessages = messages.OrderByDescending(m => m.Timestamp).Take(20)
                                          .Select(m => new ResponseMessageDto { Id = m.Id, Content = m.Content, Timestamp = m.Timestamp }).ToList();
            
            _mockMessageRepository.Setup(repo => repo.GetAllAsync(null)).ReturnsAsync(messages); // No filter expression for this overload
            _mockMapper.Setup(m => m.Map<IEnumerable<ResponseMessageDto>>(It.IsAny<IEnumerable<Message>>()))
                       .Returns((IEnumerable<Message> src) => src.Select(m => new ResponseMessageDto { Id = m.Id, Content = m.Content, Timestamp = m.Timestamp }));


            // Act
            var result = await _messageService.GetMessagesAsync(getMessagesDto);

            // Assert
            _mockMessageRepository.Verify(repo => repo.GetAllAsync(null), Times.Once);
            result.Should().NotBeNull();
            result.Should().HaveCount(20); // Default PageSize
            result.Should().BeInDescendingOrder(r => r.Timestamp); // Default SortOrder
            result.First().Content.Should().Be("Message 1"); // Newest
        }

        [Fact]
        public async Task GetMessagesAsync_WithQuery_ReturnsFilteredMessages()
        {
            // Arrange
            var query = "Special";
            var getMessagesDto = new GetMessagesDto { Query = query };
            var messages = new List<Message>
            {
                new Message { Id = 1, Content = "This is a Special message", Timestamp = DateTime.UtcNow.AddMinutes(-1) },
                new Message { Id = 2, Content = "Another message", Timestamp = DateTime.UtcNow.AddMinutes(-2) },
                new Message { Id = 3, Content = "Yet another Special one", Timestamp = DateTime.UtcNow.AddMinutes(-3) }
            };
            var filteredMessages = messages.Where(m => m.Content.Contains(query)).ToList();
            var expectedResponseDtos = filteredMessages.Select(m => new ResponseMessageDto { Id = m.Id, Content = m.Content, Timestamp = m.Timestamp }).ToList();

            _mockMessageRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Message, bool>>>()))
                                 .ReturnsAsync((Expression<Func<Message, bool>> predicate) => messages.Where(predicate.Compile()).ToList());
            _mockMapper.Setup(m => m.Map<IEnumerable<ResponseMessageDto>>(It.IsAny<IEnumerable<Message>>()))
                       .Returns((IEnumerable<Message> src) => src.Select(m => new ResponseMessageDto { Id = m.Id, Content = m.Content, Timestamp = m.Timestamp }));

            // Act
            var result = await _messageService.GetMessagesAsync(getMessagesDto);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(r => r.Content.Contains(query)).Should().BeTrue();
            result.Should().BeEquivalentTo(expectedResponseDtos, options => options.ExcludingMissingMembers());
        }

        [Fact]
        public async Task GetMessagesAsync_WithCountAndSort_ReturnsCorrectly()
        {
            // Arrange
            var getMessagesDto = new GetMessagesDto { PageSize = 2, SortOrder = "asc" };
            var messages = new List<Message>
            {
                new Message { Id = 1, Content = "Message 1", Timestamp = DateTime.UtcNow.AddMinutes(-3) }, // Oldest
                new Message { Id = 2, Content = "Message 2", Timestamp = DateTime.UtcNow.AddMinutes(-2) },
                new Message { Id = 3, Content = "Message 3", Timestamp = DateTime.UtcNow.AddMinutes(-1) }  // Newest
            };
             var expectedMessages = messages.OrderBy(m => m.Timestamp).Take(2)
                                          .Select(m => new ResponseMessageDto { Id = m.Id, Content = m.Content, Timestamp = m.Timestamp }).ToList();


            _mockMessageRepository.Setup(repo => repo.GetAllAsync(null)).ReturnsAsync(messages);
             _mockMapper.Setup(m => m.Map<IEnumerable<ResponseMessageDto>>(It.IsAny<IEnumerable<Message>>()))
                       .Returns((IEnumerable<Message> src) => src.Select(m => new ResponseMessageDto { Id = m.Id, Content = m.Content, Timestamp = m.Timestamp }));


            // Act
            var result = await _messageService.GetMessagesAsync(getMessagesDto);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeInAscendingOrder(r => r.Timestamp);
            result.First().Content.Should().Be("Message 1"); // Oldest because of asc sort
            result.Should().BeEquivalentTo(expectedMessages, options => options.ExcludingMissingMembers());
        }
    }
}
