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
    public class GroupChatControllerTests
    {
        private readonly Mock<IGroupService> _mockGroupService;
        private readonly Mock<ILogger<GroupChatController>> _mockLogger;
        private readonly GroupChatController _controller;

        public GroupChatControllerTests()
        {
            _mockGroupService = new Mock<IGroupService>();
            _mockLogger = new Mock<ILogger<GroupChatController>>();
            _controller = new GroupChatController(_mockGroupService.Object, _mockLogger.Object);
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

        // 1. CreateGroup_ValidModel_ReturnsOk
        [Fact]
        public async Task CreateGroup_ValidModel_ReturnsOk()
        {
            // Arrange
            var creatorUserId = Guid.NewGuid().ToString();
            SetupUserContext(creatorUserId);
            var groupDto = new GroupDto { Name = "Test Group", Description = "A cool group" };
            var expectedResponse = new ResponseGroupDto { Id = Guid.NewGuid(), Name = groupDto.Name, Description = groupDto.Description, CreatedBy = Guid.Parse(creatorUserId) };
            _mockGroupService.Setup(s => s.CreateGroupAsync(groupDto, creatorUserId)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateGroup(groupDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
            _mockGroupService.Verify(s => s.CreateGroupAsync(groupDto, creatorUserId), Times.Once);
        }

        // 2. CreateGroup_InvalidModel_ReturnsBadRequest
        [Fact]
        public async Task CreateGroup_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var creatorUserId = Guid.NewGuid().ToString();
            SetupUserContext(creatorUserId);
            _controller.ModelState.AddModelError("Name", "Name is required");
            var groupDto = new GroupDto { Description = "A group without a name" }; // Invalid

            // Act
            var result = await _controller.CreateGroup(groupDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _mockGroupService.Verify(s => s.CreateGroupAsync(It.IsAny<GroupDto>(), It.IsAny<string>()), Times.Never);
        }

        // 3. CreateGroup_CreatorNotFound_ReturnsNotFound
        [Fact]
        public async Task CreateGroup_CreatorNotFound_ReturnsNotFound()
        {
            // Arrange
            var creatorUserId = Guid.NewGuid().ToString();
            SetupUserContext(creatorUserId);
            var groupDto = new GroupDto { Name = "Test Group" };
            var exceptionMessage = $"Creator with ID {creatorUserId} not found.";
            _mockGroupService.Setup(s => s.CreateGroupAsync(groupDto, creatorUserId)).ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.CreateGroup(groupDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        // 4. UpdateGroup_ValidModel_ReturnsOk
        [Fact]
        public async Task UpdateGroup_ValidModel_ReturnsOk()
        {
            // Arrange
            var updaterUserId = Guid.NewGuid().ToString();
            SetupUserContext(updaterUserId);
            var groupId = Guid.NewGuid();
            var groupDto = new GroupDto { Name = "Updated Group", Description = "Updated description" };
            var expectedResponse = new ResponseGroupDto { Id = groupId, Name = groupDto.Name, Description = groupDto.Description, CreatedBy = Guid.Parse(updaterUserId) };
            _mockGroupService.Setup(s => s.UpdateGroupAsync(groupId, groupDto, updaterUserId)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.UpdateGroup(groupId, groupDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
        }

        // 5. UpdateGroup_InvalidModel_ReturnsBadRequest
        [Fact]
        public async Task UpdateGroup_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var updaterUserId = Guid.NewGuid().ToString();
            SetupUserContext(updaterUserId);
            var groupId = Guid.NewGuid();
            _controller.ModelState.AddModelError("Name", "Name is required");
            var groupDto = new GroupDto { Description = "Updated description" }; // Invalid

            // Act
            var result = await _controller.UpdateGroup(groupId, groupDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _mockGroupService.Verify(s => s.UpdateGroupAsync(It.IsAny<Guid>(), It.IsAny<GroupDto>(), It.IsAny<string>()), Times.Never);
        }

        // 6. UpdateGroup_GroupNotFound_ReturnsNotFound
        [Fact]
        public async Task UpdateGroup_GroupNotFound_ReturnsNotFound()
        {
            // Arrange
            var updaterUserId = Guid.NewGuid().ToString();
            SetupUserContext(updaterUserId);
            var groupId = Guid.NewGuid();
            var groupDto = new GroupDto { Name = "Updated Group" };
            var exceptionMessage = $"Group with ID {groupId} not found.";
            _mockGroupService.Setup(s => s.UpdateGroupAsync(groupId, groupDto, updaterUserId)).ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.UpdateGroup(groupId, groupDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        // 7. UpdateGroup_Unauthorized_ReturnsUnauthorized
        [Fact]
        public async Task UpdateGroup_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var updaterUserId = Guid.NewGuid().ToString();
            SetupUserContext(updaterUserId);
            var groupId = Guid.NewGuid();
            var groupDto = new GroupDto { Name = "Updated Group" };
            var exceptionMessage = "You are not authorized to update this group.";
            _mockGroupService.Setup(s => s.UpdateGroupAsync(groupId, groupDto, updaterUserId)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

            // Act
            var result = await _controller.UpdateGroup(groupId, groupDto);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
            unauthorizedResult.Value.Should().Be(exceptionMessage);
        }

        // 8. DeleteGroup_ValidRequest_ReturnsNoContent
        [Fact]
        public async Task DeleteGroup_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var deleterUserId = Guid.NewGuid().ToString();
            SetupUserContext(deleterUserId);
            var groupId = Guid.NewGuid();
            _mockGroupService.Setup(s => s.DeleteGroupAsync(groupId, deleterUserId)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteGroup(groupId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        // 9. DeleteGroup_GroupNotFound_ReturnsNotFound
        [Fact]
        public async Task DeleteGroup_GroupNotFound_ReturnsNotFound()
        {
            // Arrange
            var deleterUserId = Guid.NewGuid().ToString();
            SetupUserContext(deleterUserId);
            var groupId = Guid.NewGuid();
            var exceptionMessage = $"Group with ID {groupId} not found.";
            _mockGroupService.Setup(s => s.DeleteGroupAsync(groupId, deleterUserId)).ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.DeleteGroup(groupId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        // 10. DeleteGroup_Unauthorized_ReturnsUnauthorized
        [Fact]
        public async Task DeleteGroup_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var deleterUserId = Guid.NewGuid().ToString();
            SetupUserContext(deleterUserId);
            var groupId = Guid.NewGuid();
            var exceptionMessage = "You are not authorized to delete this group.";
            _mockGroupService.Setup(s => s.DeleteGroupAsync(groupId, deleterUserId)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

            // Act
            var result = await _controller.DeleteGroup(groupId);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
            unauthorizedResult.Value.Should().Be(exceptionMessage);
        }

        // 11. DeleteGroup_ServiceReturnsFalse_ReturnsBadRequest
        [Fact]
        public async Task DeleteGroup_ServiceReturnsFalse_ReturnsBadRequest()
        {
            // Arrange
            var deleterUserId = Guid.NewGuid().ToString();
            SetupUserContext(deleterUserId);
            var groupId = Guid.NewGuid();
            _mockGroupService.Setup(s => s.DeleteGroupAsync(groupId, deleterUserId)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteGroup(groupId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("Failed to delete group.");
        }

        // 12. AddMemberToGroup_ValidRequest_ReturnsOk
        [Fact]
        public async Task AddMemberToGroup_ValidRequest_ReturnsOk()
        {
            // Arrange
            var adminUserId = Guid.NewGuid().ToString();
            SetupUserContext(adminUserId);
            var groupId = Guid.NewGuid();
            var memberDto = new AddGroupMemberDto { UserId = Guid.NewGuid().ToString() };
            _mockGroupService.Setup(s => s.AddMemberToGroupAsync(groupId, memberDto, adminUserId)).ReturnsAsync(true);

            // Act
            var result = await _controller.AddMemberToGroup(groupId, memberDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be("Member added successfully.");
        }

        // 13. AddMemberToGroup_InvalidModel_ReturnsBadRequest
        [Fact]
        public async Task AddMemberToGroup_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var adminUserId = Guid.NewGuid().ToString();
            SetupUserContext(adminUserId);
            var groupId = Guid.NewGuid();
            _controller.ModelState.AddModelError("UserId", "UserId is required");
            var memberDto = new AddGroupMemberDto { }; // Invalid

            // Act
            var result = await _controller.AddMemberToGroup(groupId, memberDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // 14. AddMemberToGroup_GroupOrUserNotFound_ReturnsNotFound
        [Fact]
        public async Task AddMemberToGroup_GroupOrUserNotFound_ReturnsNotFound()
        {
            // Arrange
            var adminUserId = Guid.NewGuid().ToString();
            SetupUserContext(adminUserId);
            var groupId = Guid.NewGuid();
            var memberDto = new AddGroupMemberDto { UserId = Guid.NewGuid().ToString() };
            var exceptionMessage = "Group or User not found.";
            _mockGroupService.Setup(s => s.AddMemberToGroupAsync(groupId, memberDto, adminUserId)).ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.AddMemberToGroup(groupId, memberDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        // 15. AddMemberToGroup_UserAlreadyMember_ReturnsConflict
        [Fact]
        public async Task AddMemberToGroup_UserAlreadyMember_ReturnsConflict()
        {
            // Arrange
            var adminUserId = Guid.NewGuid().ToString();
            SetupUserContext(adminUserId);
            var groupId = Guid.NewGuid();
            var memberDto = new AddGroupMemberDto { UserId = Guid.NewGuid().ToString() };
            var exceptionMessage = "User is already a member.";
            _mockGroupService.Setup(s => s.AddMemberToGroupAsync(groupId, memberDto, adminUserId)).ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _controller.AddMemberToGroup(groupId, memberDto);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.Value.Should().Be(exceptionMessage);
        }

        // 16. AddMemberToGroup_Unauthorized_ReturnsUnauthorized
        [Fact]
        public async Task AddMemberToGroup_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var nonAdminUserId = Guid.NewGuid().ToString();
            SetupUserContext(nonAdminUserId);
            var groupId = Guid.NewGuid();
            var memberDto = new AddGroupMemberDto { UserId = Guid.NewGuid().ToString() };
            var exceptionMessage = "Not authorized to add members.";
            _mockGroupService.Setup(s => s.AddMemberToGroupAsync(groupId, memberDto, nonAdminUserId)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

            // Act
            var result = await _controller.AddMemberToGroup(groupId, memberDto);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().Be(exceptionMessage);
        }

        // 17. RemoveMemberFromGroup_ValidRequest_ReturnsOk
        [Fact]
        public async Task RemoveMemberFromGroup_ValidRequest_ReturnsOk()
        {
            // Arrange
            var adminOrMemberUserId = Guid.NewGuid().ToString();
            SetupUserContext(adminOrMemberUserId);
            var groupId = Guid.NewGuid();
            var memberId = Guid.NewGuid().ToString();
            _mockGroupService.Setup(s => s.RemoveMemberFromGroupAsync(groupId, memberId, adminOrMemberUserId)).ReturnsAsync(true);

            // Act
            var result = await _controller.RemoveMemberFromGroup(groupId, memberId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be("Member removed successfully.");
        }

        // 18. RemoveMemberFromGroup_GroupOrMemberNotFound_ReturnsNotFound
        [Fact]
        public async Task RemoveMemberFromGroup_GroupOrMemberNotFound_ReturnsNotFound()
        {
            // Arrange
            var adminOrMemberUserId = Guid.NewGuid().ToString();
            SetupUserContext(adminOrMemberUserId);
            var groupId = Guid.NewGuid();
            var memberId = Guid.NewGuid().ToString();
            var exceptionMessage = "Group or Member not found.";
            _mockGroupService.Setup(s => s.RemoveMemberFromGroupAsync(groupId, memberId, adminOrMemberUserId)).ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.RemoveMemberFromGroup(groupId, memberId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        // 19. RemoveMemberFromGroup_Unauthorized_ReturnsUnauthorized
        [Fact]
        public async Task RemoveMemberFromGroup_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var nonAdminNonMemberUserId = Guid.NewGuid().ToString();
            SetupUserContext(nonAdminNonMemberUserId);
            var groupId = Guid.NewGuid();
            var memberId = Guid.NewGuid().ToString();
            var exceptionMessage = "Not authorized to remove member.";
            _mockGroupService.Setup(s => s.RemoveMemberFromGroupAsync(groupId, memberId, nonAdminNonMemberUserId)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

            // Act
            var result = await _controller.RemoveMemberFromGroup(groupId, memberId);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().Be(exceptionMessage);
        }

        // 20. GetGroupById_GroupExists_ReturnsOk
        [Fact]
        public async Task GetGroupById_GroupExists_ReturnsOk()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var expectedResponse = new ResponseGroupDto { Id = groupId, Name = "Test Group" };
            _mockGroupService.Setup(s => s.GetGroupByIdAsync(groupId)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetGroupById(groupId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
        }

        // 21. GetGroupById_GroupNotFound_ReturnsNotFound
        [Fact]
        public async Task GetGroupById_GroupNotFound_ReturnsNotFound()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var exceptionMessage = "Group not found.";
            _mockGroupService.Setup(s => s.GetGroupByIdAsync(groupId)).ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.GetGroupById(groupId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be(exceptionMessage);
        }

        // 22. GetAllGroups_ReturnsOkWithGroupList
        [Fact]
        public async Task GetAllGroups_ReturnsOkWithGroupList()
        {
            // Arrange
            var expectedResponse = new List<ResponseGroupDto>
            {
                new ResponseGroupDto { Id = Guid.NewGuid(), Name = "Group 1" },
                new ResponseGroupDto { Id = Guid.NewGuid(), Name = "Group 2" }
            };
            _mockGroupService.Setup(s => s.GetAllGroupsAsync()).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAllGroups();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
        }

        // 23. GetGroupMembers_GroupExists_ReturnsOkWithMemberList
        [Fact]
        public async Task GetGroupMembers_GroupExists_ReturnsOkWithMemberList()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var expectedResponse = new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), Name = "Member 1" },
                new UserDto { Id = Guid.NewGuid(), Name = "Member 2" }
            };
            _mockGroupService.Setup(s => s.GetGroupMembersAsync(groupId)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetGroupMembers(groupId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
        }

        // 24. GetGroupMembers_GroupNotFound_ReturnsNotFound
        [Fact]
        public async Task GetGroupMembers_GroupNotFound_ReturnsNotFound()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var exceptionMessage = "Group not found, so cannot retrieve members.";
            _mockGroupService.Setup(s => s.GetGroupMembersAsync(groupId)).ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.GetGroupMembers(groupId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be(exceptionMessage);
        }
    }
}
