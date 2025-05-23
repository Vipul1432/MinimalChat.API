using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using MinimalChat.API.Hubs;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace MinimalChat.Tests
{
    // Define IChatClient and GroupMessageDto for testing purposes
    // These should ideally mirror what's in ChatHub.cs or a shared contract
    public interface IChatClient
    {
        Task ReceiveMessage(MessageDto message);
        Task ReceiveGroupMessage(GroupMessageDto message);
        Task JoinedGroup(string groupName);
        Task LeftGroup(string groupName);
        Task UserOnline(string userId);
        Task UserOffline(string userId);
        Task Error(string message); // Added for error notifications
    }

    public class GroupMessageDto // Simplified for testing
    {
        public Guid GroupId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; } // Assuming sender name might be useful
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }


    public class ChatHubTests
    {
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IGroupService> _mockGroupService;
        private readonly Mock<IHubCallerClients<IChatClient>> _mockClients;
        private readonly Mock<IChatClient> _mockClientProxy; // Client proxy for specific users/groups/caller
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly Mock<HubCallerContext> _mockContext;
        private readonly ChatHub _hub;

        public ChatHubTests()
        {
            _mockMessageService = new Mock<IMessageService>();
            _mockGroupService = new Mock<IGroupService>();
            _mockClients = new Mock<IHubCallerClients<IChatClient>>();
            _mockClientProxy = new Mock<IChatClient>();
            _mockGroups = new Mock<IGroupManager>();
            _mockContext = new Mock<HubCallerContext>();

            _hub = new ChatHub(_mockMessageService.Object, _mockGroupService.Object)
            {
                Clients = _mockClients.Object,
                Groups = _mockGroups.Object,
                Context = _mockContext.Object
            };
        }

        private void SetupHubContext(string userId, string connectionId = "test-connection-id")
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var user = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(user);
            _mockContext.Setup(c => c.UserIdentifier).Returns(userId);
            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        }

        [Fact]
        public async Task SendMessageToUser_ValidMessage_CallsServiceAndSendsToReceiver()
        {
            // Arrange
            var senderId = Guid.NewGuid().ToString();
            var receiverId = Guid.NewGuid().ToString();
            var messageContent = "Hello there!";
            var connectionId = "sender-conn-id";
            SetupHubContext(senderId, connectionId);

            var messageDtoFromService = new ResponseMessageDto // This is what MessageService returns
            {
                Id = 1,
                SenderId = Guid.Parse(senderId),
                ReceiverId = Guid.Parse(receiverId),
                Content = messageContent,
                Timestamp = DateTime.UtcNow,
                SenderName = "Sender",
                ReceiverName = "Receiver"
            };
            
            // This is what ChatHub maps to for IChatClient.ReceiveMessage
            var expectedMessageDtoForClient = new MessageDto 
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = messageContent,
                Timestamp = messageDtoFromService.Timestamp // Assuming timestamp is passed through
            };


            _mockMessageService.Setup(s => s.SendMessageAsync(It.Is<MessageDto>(dto =>
                dto.SenderId == senderId &&
                dto.ReceiverId == receiverId &&
                dto.Content == messageContent)))
                .ReturnsAsync(messageDtoFromService);

            _mockClients.Setup(c => c.User(receiverId)).Returns(_mockClientProxy.Object);
            _mockClients.Setup(c => c.Client(connectionId)).Returns(_mockClientProxy.Object); // For sender confirmation

            // Act
            await _hub.SendMessageToUser(receiverId, messageContent);

            // Assert
            _mockMessageService.Verify(s => s.SendMessageAsync(It.Is<MessageDto>(dto =>
                dto.SenderId == senderId &&
                dto.ReceiverId == receiverId &&
                dto.Content == messageContent)), Times.Once);

            // Verify message sent to receiver
            _mockClientProxy.Verify(c => c.ReceiveMessage(It.Is<MessageDto>(m =>
                m.SenderId == senderId &&
                m.ReceiverId == receiverId &&
                m.Content == messageContent &&
                m.Timestamp == expectedMessageDtoForClient.Timestamp
            )), Times.Once);
            
            // Verify message also sent back to the sender (caller)
             _mockClientProxy.Verify(c => c.ReceiveMessage(It.Is<MessageDto>(m =>
                m.SenderId == senderId &&
                m.ReceiverId == receiverId && // Self-message still has receiverId
                m.Content == messageContent &&
                m.Timestamp == expectedMessageDtoForClient.Timestamp
            )), Times.Exactly(2)); // Once for receiver, once for sender
        }

        [Fact]
        public async Task SendMessageToUser_ServiceThrowsException_DoesNotSendMessage()
        {
            // Arrange
            var senderId = Guid.NewGuid().ToString();
            var receiverId = Guid.NewGuid().ToString();
            var messageContent = "Test message";
            SetupHubContext(senderId);

            _mockMessageService.Setup(s => s.SendMessageAsync(It.IsAny<MessageDto>()))
                .ThrowsAsync(new Exception("Service error"));

            _mockClients.Setup(c => c.User(receiverId)).Returns(_mockClientProxy.Object);
            _mockClients.Setup(c => c.Caller).Returns(_mockClientProxy.Object);


            // Act
            await Assert.ThrowsAsync<Exception>(() => _hub.SendMessageToUser(receiverId, messageContent));

            // Assert
            _mockMessageService.Verify(s => s.SendMessageAsync(It.IsAny<MessageDto>()), Times.Once);
            _mockClientProxy.Verify(c => c.ReceiveMessage(It.IsAny<MessageDto>()), Times.Never);
            // Optional: Verify error sent to caller if implemented
            // _mockClientProxy.Verify(c => c.Error(It.IsAny<string>()), Times.Once); 
        }

        [Fact]
        public async Task JoinGroup_ValidRequest_CallsServiceAndAddsToGroup()
        {
            // Arrange
            var memberId = Guid.NewGuid().ToString();
            var connectionId = "test-conn-id";
            var groupId = Guid.NewGuid();
            var groupName = "Test Group";
            SetupHubContext(memberId, connectionId);

            _mockGroupService.Setup(s => s.AddMemberToGroupAsync(groupId, 
                                     It.Is<AddGroupMemberDto>(dto => dto.UserId == memberId), 
                                     memberId)) // currentUserId is the memberId itself
                .ReturnsAsync(true);

            _mockClients.Setup(c => c.Caller).Returns(_mockClientProxy.Object);

            // Act
            await _hub.JoinGroup(groupId.ToString(), groupName);

            // Assert
            _mockGroupService.Verify(s => s.AddMemberToGroupAsync(groupId, 
                                      It.Is<AddGroupMemberDto>(dto => dto.UserId == memberId),
                                      memberId), Times.Once);
            _mockGroups.Verify(g => g.AddToGroupAsync(connectionId, groupName, default), Times.Once);
            _mockClientProxy.Verify(c => c.JoinedGroup(groupName), Times.Once);
        }

        [Fact]
        public async Task JoinGroup_ServiceFails_DoesNotAddToGroupOrNotify()
        {
            // Arrange
            var memberId = Guid.NewGuid().ToString();
            var connectionId = "test-conn-id";
            var groupId = Guid.NewGuid();
            var groupName = "Test Group";
            SetupHubContext(memberId, connectionId);

            _mockGroupService.Setup(s => s.AddMemberToGroupAsync(groupId, It.Is<AddGroupMemberDto>(dto => dto.UserId == memberId), memberId))
                .ReturnsAsync(false); // Service indicates failure

            _mockClients.Setup(c => c.Caller).Returns(_mockClientProxy.Object);

            // Act
            await _hub.JoinGroup(groupId.ToString(), groupName); // Should not throw, but handle gracefully

            // Assert
            _mockGroupService.Verify(s => s.AddMemberToGroupAsync(groupId, It.Is<AddGroupMemberDto>(dto => dto.UserId == memberId), memberId), Times.Once);
            _mockGroups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
            _mockClientProxy.Verify(c => c.JoinedGroup(It.IsAny<string>()), Times.Never);
            // Verify error notification to caller
            _mockClientProxy.Verify(c => c.Error($"Failed to join group {groupName}."), Times.Once);
        }
        
        [Fact]
        public async Task SendMessageToGroup_ValidMessage_SendsToGroup()
        {
            // Arrange
            var senderId = Guid.NewGuid().ToString();
            var senderName = "Test Sender"; // Assume hub fetches this or it's passed
            var groupId = Guid.NewGuid();
            var groupName = "Test Chat Group";
            var messageContent = "Hello group!";
            SetupHubContext(senderId);

             _mockContext.Setup(c => c.User.Identity.Name).Returns(senderName); // If hub uses User.Identity.Name

            var expectedGroupMessage = new GroupMessageDto
            {
                GroupId = groupId,
                SenderId = senderId,
                SenderName = senderName, 
                Content = messageContent,
                Timestamp = It.IsAny<DateTime>() // Timestamp is generated in hub
            };
            
            // For group messages, we don't expect MessageService to be called based on ChatHub's current implementation
            // If it were, we'd mock _mockMessageService.SendMessageAsync here.

            _mockClients.Setup(c => c.Group(groupName)).Returns(_mockClientProxy.Object);

            // Act
            await _hub.SendMessageToGroup(groupId.ToString(), groupName, messageContent);

            // Assert
            _mockClientProxy.Verify(c => c.ReceiveGroupMessage(It.Is<GroupMessageDto>(dto =>
                dto.GroupId == groupId &&
                dto.SenderId == senderId &&
                dto.SenderName == senderName && // Or mock User.Identity.Name if used
                dto.Content == messageContent
            )), Times.Once);
        }


        [Fact]
        public async Task LeaveGroup_ValidRequest_CallsServiceAndRemovesFromGroup()
        {
            // Arrange
            var memberId = Guid.NewGuid().ToString();
            var connectionId = "test-conn-id";
            var groupId = Guid.NewGuid();
            var groupName = "Test Group";
            SetupHubContext(memberId, connectionId);

            _mockGroupService.Setup(s => s.RemoveMemberFromGroupAsync(groupId, memberId, memberId))
                .ReturnsAsync(true);

            _mockClients.Setup(c => c.Caller).Returns(_mockClientProxy.Object);

            // Act
            await _hub.LeaveGroup(groupId.ToString(), groupName);

            // Assert
            _mockGroupService.Verify(s => s.RemoveMemberFromGroupAsync(groupId, memberId, memberId), Times.Once);
            _mockGroups.Verify(g => g.RemoveFromGroupAsync(connectionId, groupName, default), Times.Once);
            _mockClientProxy.Verify(c => c.LeftGroup(groupName), Times.Once);
        }
        
        [Fact]
        public async Task LeaveGroup_ServiceFails_DoesNotRemoveFromGroupOrNotify()
        {
            // Arrange
            var memberId = Guid.NewGuid().ToString();
            var connectionId = "test-conn-id";
            var groupId = Guid.NewGuid();
            var groupName = "Test Group";
            SetupHubContext(memberId, connectionId);

            _mockGroupService.Setup(s => s.RemoveMemberFromGroupAsync(groupId, memberId, memberId))
                .ReturnsAsync(false); // Service indicates failure

            _mockClients.Setup(c => c.Caller).Returns(_mockClientProxy.Object);

            // Act
            await _hub.LeaveGroup(groupId.ToString(), groupName);

            // Assert
            _mockGroupService.Verify(s => s.RemoveMemberFromGroupAsync(groupId, memberId, memberId), Times.Once);
            _mockGroups.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
            _mockClientProxy.Verify(c => c.LeftGroup(It.IsAny<string>()), Times.Never);
            // Verify error notification to caller
            _mockClientProxy.Verify(c => c.Error($"Failed to leave group {groupName}."), Times.Once);
        }
    }
}
