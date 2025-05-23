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
    public class GroupServiceTests
    {
        private readonly Mock<IRepository<Group>> _mockGroupRepository;
        private readonly Mock<IRepository<User>> _mockUserRepository;
        private readonly Mock<IRepository<GroupMember>> _mockGroupMemberRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly GroupService _groupService;

        public GroupServiceTests()
        {
            _mockGroupRepository = new Mock<IRepository<Group>>();
            _mockUserRepository = new Mock<IRepository<User>>();
            _mockGroupMemberRepository = new Mock<IRepository<GroupMember>>();
            _mockMapper = new Mock<IMapper>();
            _groupService = new GroupService(
                _mockGroupRepository.Object,
                _mockUserRepository.Object,
                _mockGroupMemberRepository.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task CreateGroupAsync_Success()
        {
            // Arrange
            var creatorId = Guid.NewGuid().ToString();
            var groupDto = new GroupDto { Name = "Test Group", Description = "Test Description" };
            var creator = new User { Id = Guid.Parse(creatorId), Name = "Creator" };
            var group = new Group { Id = Guid.NewGuid(), Name = groupDto.Name, Description = groupDto.Description, CreatedBy = Guid.Parse(creatorId) };
            var responseGroupDto = new ResponseGroupDto { Id = group.Id, Name = group.Name, Description = group.Description, CreatedBy = group.CreatedBy };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(Guid.Parse(creatorId))).ReturnsAsync(creator);
            _mockMapper.Setup(m => m.Map<Group>(groupDto)).Returns(group);
            _mockGroupRepository.Setup(repo => repo.AddAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);
            _mockGroupMemberRepository.Setup(repo => repo.AddAsync(It.IsAny<GroupMember>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<ResponseGroupDto>(It.IsAny<Group>())).Returns(responseGroupDto);

            // Act
            var result = await _groupService.CreateGroupAsync(groupDto, creatorId);

            // Assert
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(Guid.Parse(creatorId)), Times.Once);
            _mockGroupRepository.Verify(repo => repo.AddAsync(It.Is<Group>(g => g.Name == groupDto.Name && g.CreatedBy == Guid.Parse(creatorId))), Times.Once);
            _mockGroupMemberRepository.Verify(repo => repo.AddAsync(It.Is<GroupMember>(gm => gm.GroupId == group.Id && gm.UserId == Guid.Parse(creatorId))), Times.Once);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(responseGroupDto);
        }

        [Fact]
        public async Task CreateGroupAsync_CreatorNotFound()
        {
            // Arrange
            var creatorId = Guid.NewGuid().ToString();
            var groupDto = new GroupDto { Name = "Test Group" };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(Guid.Parse(creatorId))).ReturnsAsync((User)null);

            // Act
            Func<Task> act = async () => await _groupService.CreateGroupAsync(groupDto, creatorId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Creator with ID {creatorId} not found.");
        }

        [Fact]
        public async Task UpdateGroupAsync_Success()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString();
            var groupDto = new GroupDto { Name = "Updated Group Name", Description = "Updated Description" };
            var existingGroup = new Group { Id = groupId, Name = "Old Name", Description = "Old Desc", CreatedBy = Guid.Parse(currentUserId) };
            var updatedGroup = new Group { Id = groupId, Name = groupDto.Name, Description = groupDto.Description, CreatedBy = Guid.Parse(currentUserId) };
            var responseGroupDto = new ResponseGroupDto { Id = groupId, Name = updatedGroup.Name, Description = updatedGroup.Description, CreatedBy = Guid.Parse(currentUserId) };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(existingGroup);
            _mockMapper.Setup(m => m.Map(groupDto, existingGroup)).Returns(updatedGroup); // Simulate AutoMapper updating existingGroup
            _mockGroupRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<ResponseGroupDto>(It.IsAny<Group>())).Returns(responseGroupDto);


            // Act
            var result = await _groupService.UpdateGroupAsync(groupId, groupDto, currentUserId);

            // Assert
            _mockGroupRepository.Verify(repo => repo.GetByIdAsync(groupId), Times.Once);
            _mockGroupRepository.Verify(repo => repo.UpdateAsync(It.Is<Group>(g => g.Name == groupDto.Name && g.Description == groupDto.Description)), Times.Once);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(responseGroupDto);
        }

        [Fact]
        public async Task UpdateGroupAsync_GroupNotFound()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var groupDto = new GroupDto { Name = "Updated Name" };
            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync((Group)null);

            // Act
            Func<Task> act = async () => await _groupService.UpdateGroupAsync(groupId, groupDto, Guid.NewGuid().ToString());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Group with ID {groupId} not found.");
        }

        [Fact]
        public async Task UpdateGroupAsync_Unauthorized()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString(); // Different user
            var groupDto = new GroupDto { Name = "Updated Name" };
            var existingGroup = new Group { Id = groupId, Name = "Old Name", CreatedBy = ownerUserId };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(existingGroup);

            // Act
            Func<Task> act = async () => await _groupService.UpdateGroupAsync(groupId, groupDto, currentUserId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You are not authorized to update this group.");
        }

        [Fact]
        public async Task DeleteGroupAsync_Success()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString();
            var group = new Group { Id = groupId, CreatedBy = Guid.Parse(currentUserId) };
            var groupMembers = new List<GroupMember> { new GroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = Guid.NewGuid() } };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);
            _mockGroupMemberRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<GroupMember, bool>>>())).ReturnsAsync(groupMembers);
            _mockGroupMemberRepository.Setup(repo => repo.DeleteAsync(It.IsAny<GroupMember>())).Returns(Task.CompletedTask);
            _mockGroupRepository.Setup(repo => repo.DeleteAsync(group)).Returns(Task.CompletedTask);

            // Act
            var result = await _groupService.DeleteGroupAsync(groupId, currentUserId);

            // Assert
            _mockGroupRepository.Verify(repo => repo.GetByIdAsync(groupId), Times.Once);
            _mockGroupMemberRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<GroupMember, bool>>>()), Times.Once);
            _mockGroupMemberRepository.Verify(repo => repo.DeleteAsync(It.IsAny<GroupMember>()), Times.Exactly(groupMembers.Count));
            _mockGroupRepository.Verify(repo => repo.DeleteAsync(group), Times.Once);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteGroupAsync_GroupNotFound()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync((Group)null);

            // Act
            Func<Task> act = async () => await _groupService.DeleteGroupAsync(groupId, Guid.NewGuid().ToString());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Group with ID {groupId} not found.");
        }

        [Fact]
        public async Task DeleteGroupAsync_Unauthorized()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString(); // Different user
            var group = new Group { Id = groupId, CreatedBy = ownerUserId };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);

            // Act
            Func<Task> act = async () => await _groupService.DeleteGroupAsync(groupId, currentUserId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You are not authorized to delete this group.");
        }

        [Fact]
        public async Task AddMemberToGroupAsync_Success()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userIdToAdd = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString(); // Admin
            var addGroupMemberDto = new AddGroupMemberDto { UserId = userIdToAdd.ToString() };
            var group = new Group { Id = groupId, CreatedBy = Guid.Parse(currentUserId) };
            var userToAdd = new User { Id = userIdToAdd };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userIdToAdd)).ReturnsAsync(userToAdd);
            _mockGroupMemberRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupMember, bool>>>())).ReturnsAsync((GroupMember)null); // Not already a member
            _mockGroupMemberRepository.Setup(repo => repo.AddAsync(It.IsAny<GroupMember>())).Returns(Task.CompletedTask);

            // Act
            var result = await _groupService.AddMemberToGroupAsync(groupId, addGroupMemberDto, currentUserId);

            // Assert
            _mockGroupRepository.Verify(repo => repo.GetByIdAsync(groupId), Times.Once);
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(userIdToAdd), Times.Once);
            _mockGroupMemberRepository.Verify(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupMember, bool>>>()), Times.Once);
            _mockGroupMemberRepository.Verify(repo => repo.AddAsync(It.Is<GroupMember>(gm => gm.GroupId == groupId && gm.UserId == userIdToAdd)), Times.Once);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AddMemberToGroupAsync_GroupNotFound()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var addGroupMemberDto = new AddGroupMemberDto { UserId = Guid.NewGuid().ToString() };
            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync((Group)null);

            // Act
            Func<Task> act = async () => await _groupService.AddMemberToGroupAsync(groupId, addGroupMemberDto, Guid.NewGuid().ToString());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Group with ID {groupId} not found.");
        }

        [Fact]
        public async Task AddMemberToGroupAsync_UserToAddNotFound()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userIdToAdd = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString();
            var addGroupMemberDto = new AddGroupMemberDto { UserId = userIdToAdd.ToString() };
            var group = new Group { Id = groupId, CreatedBy = Guid.Parse(currentUserId) };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userIdToAdd)).ReturnsAsync((User)null);

            // Act
            Func<Task> act = async () => await _groupService.AddMemberToGroupAsync(groupId, addGroupMemberDto, currentUserId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"User with ID {userIdToAdd} not found.");
        }

        [Fact]
        public async Task AddMemberToGroupAsync_UserAlreadyMember()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userIdToAdd = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString();
            var addGroupMemberDto = new AddGroupMemberDto { UserId = userIdToAdd.ToString() };
            var group = new Group { Id = groupId, CreatedBy = Guid.Parse(currentUserId) };
            var userToAdd = new User { Id = userIdToAdd };
            var existingMember = new GroupMember { GroupId = groupId, UserId = userIdToAdd };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userIdToAdd)).ReturnsAsync(userToAdd);
            _mockGroupMemberRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupMember, bool>>>())).ReturnsAsync(existingMember);

            // Act
            Func<Task> act = async () => await _groupService.AddMemberToGroupAsync(groupId, addGroupMemberDto, currentUserId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"User {userIdToAdd} is already a member of group {groupId}.");
        }

        [Fact]
        public async Task AddMemberToGroupAsync_UnauthorizedNotAdmin()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userIdToAdd = Guid.NewGuid();
            var groupAdminId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString(); // Not admin
            var addGroupMemberDto = new AddGroupMemberDto { UserId = userIdToAdd.ToString() };
            var group = new Group { Id = groupId, CreatedBy = groupAdminId };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);

            // Act
            Func<Task> act = async () => await _groupService.AddMemberToGroupAsync(groupId, addGroupMemberDto, currentUserId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You are not authorized to add members to this group.");
        }

        [Fact]
        public async Task RemoveMemberFromGroupAsync_Success_AdminRemoves()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var memberIdToRemove = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString(); // Admin
            var group = new Group { Id = groupId, CreatedBy = Guid.Parse(currentUserId) };
            var userToRemove = new User { Id = memberIdToRemove };
            var groupMemberEntry = new GroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = memberIdToRemove };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(memberIdToRemove)).ReturnsAsync(userToRemove);
            _mockGroupMemberRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupMember, bool>>>())).ReturnsAsync(groupMemberEntry);
            _mockGroupMemberRepository.Setup(repo => repo.DeleteAsync(groupMemberEntry)).Returns(Task.CompletedTask);

            // Act
            var result = await _groupService.RemoveMemberFromGroupAsync(groupId, memberIdToRemove.ToString(), currentUserId);

            // Assert
            _mockGroupRepository.Verify(repo => repo.GetByIdAsync(groupId), Times.Once);
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(memberIdToRemove), Times.Once);
            _mockGroupMemberRepository.Verify(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupMember, bool>>>()), Times.Once);
            _mockGroupMemberRepository.Verify(repo => repo.DeleteAsync(groupMemberEntry), Times.Once);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveMemberFromGroupAsync_Success_MemberRemovesSelf()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var memberIdToRemove = Guid.NewGuid();
            var currentUserId = memberIdToRemove.ToString(); // Member removing self
            var groupAdminId = Guid.NewGuid(); // Different from current user
            var group = new Group { Id = groupId, CreatedBy = groupAdminId };
            var userToRemove = new User { Id = memberIdToRemove };
            var groupMemberEntry = new GroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = memberIdToRemove };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(memberIdToRemove)).ReturnsAsync(userToRemove);
            _mockGroupMemberRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupMember, bool>>>())).ReturnsAsync(groupMemberEntry);
            _mockGroupMemberRepository.Setup(repo => repo.DeleteAsync(groupMemberEntry)).Returns(Task.CompletedTask);

            // Act
            var result = await _groupService.RemoveMemberFromGroupAsync(groupId, memberIdToRemove.ToString(), currentUserId);

            // Assert
            result.Should().BeTrue();
        }


        [Fact]
        public async Task RemoveMemberFromGroupAsync_GroupNotFound()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync((Group)null);

            // Act
            Func<Task> act = async () => await _groupService.RemoveMemberFromGroupAsync(groupId, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Group with ID {groupId} not found.");
        }

        [Fact]
        public async Task RemoveMemberFromGroupAsync_MemberNotFoundInGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var memberIdToRemove = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString();
            var group = new Group { Id = groupId, CreatedBy = Guid.Parse(currentUserId) }; // Current user is admin
            var userToRemove = new User { Id = memberIdToRemove };


            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(memberIdToRemove)).ReturnsAsync(userToRemove);
            _mockGroupMemberRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupMember, bool>>>())).ReturnsAsync((GroupMember)null); // Member not in group

            // Act
            Func<Task> act = async () => await _groupService.RemoveMemberFromGroupAsync(groupId, memberIdToRemove.ToString(), currentUserId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Member with ID {memberIdToRemove} not found in group {groupId}.");
        }

        [Fact]
        public async Task RemoveMemberFromGroupAsync_Unauthorized()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var memberIdToRemove = Guid.NewGuid();
            var groupAdminId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid().ToString(); // Neither admin nor member being removed
            var group = new Group { Id = groupId, CreatedBy = groupAdminId };
            var userToRemove = new User { Id = memberIdToRemove };
            var groupMemberEntry = new GroupMember { GroupId = groupId, UserId = memberIdToRemove };


            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(memberIdToRemove)).ReturnsAsync(userToRemove);
            _mockGroupMemberRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupMember, bool>>>())).ReturnsAsync(groupMemberEntry);

            // Act
            Func<Task> act = async () => await _groupService.RemoveMemberFromGroupAsync(groupId, memberIdToRemove.ToString(), currentUserId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You are not authorized to remove this member.");
        }

        [Fact]
        public async Task GetGroupByIdAsync_GroupFound()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var group = new Group { Id = groupId, Name = "Test Group" };
            var responseGroupDto = new ResponseGroupDto { Id = groupId, Name = "Test Group" };

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);
            _mockMapper.Setup(m => m.Map<ResponseGroupDto>(group)).Returns(responseGroupDto);

            // Act
            var result = await _groupService.GetGroupByIdAsync(groupId);

            // Assert
            _mockGroupRepository.Verify(repo => repo.GetByIdAsync(groupId), Times.Once);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(responseGroupDto);
        }

        [Fact]
        public async Task GetGroupByIdAsync_GroupNotFound()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync((Group)null);

            // Act
            Func<Task> act = async () => await _groupService.GetGroupByIdAsync(groupId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Group with ID {groupId} not found.");
        }

        [Fact]
        public async Task GetAllGroupsAsync_ReturnsGroups()
        {
            // Arrange
            var groups = new List<Group>
            {
                new Group { Id = Guid.NewGuid(), Name = "Group 1" },
                new Group { Id = Guid.NewGuid(), Name = "Group 2" }
            };
            var responseGroupDtos = groups.Select(g => new ResponseGroupDto { Id = g.Id, Name = g.Name }).ToList();

            _mockGroupRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(groups);
            _mockMapper.Setup(m => m.Map<IEnumerable<ResponseGroupDto>>(groups)).Returns(responseGroupDtos);

            // Act
            var result = await _groupService.GetAllGroupsAsync();

            // Assert
            _mockGroupRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
            result.Should().NotBeNull();
            result.Should().HaveCount(groups.Count);
            result.Should().BeEquivalentTo(responseGroupDtos);
        }

        [Fact]
        public async Task GetGroupMembersAsync_GroupFound_ReturnsMembers()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var group = new Group { Id = groupId, Name = "Test Group" };
            var memberUserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var groupMembers = memberUserIds.Select(uid => new GroupMember { GroupId = groupId, UserId = uid }).ToList();
            var users = memberUserIds.Select(uid => new User { Id = uid, Name = $"User {uid}" }).ToList();
            var userDtos = users.Select(u => new UserDto { Id = u.Id, Name = u.Name }).ToList();

            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync(group);
            _mockGroupMemberRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<GroupMember, bool>>>()))
                .ReturnsAsync(groupMembers);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => users.FirstOrDefault(u => u.Id == id));
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
                       .Returns((User u) => userDtos.FirstOrDefault(dto => dto.Id == u.Id));


            // Act
            var result = await _groupService.GetGroupMembersAsync(groupId);

            // Assert
            _mockGroupRepository.Verify(repo => repo.GetByIdAsync(groupId), Times.Once);
            _mockGroupMemberRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<GroupMember, bool>>>()), Times.Once);
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Exactly(memberUserIds.Count));
            result.Should().NotBeNull();
            result.Should().HaveCount(memberUserIds.Count);
            result.Should().BeEquivalentTo(userDtos);
        }

        [Fact]
        public async Task GetGroupMembersAsync_GroupNotFound()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            _mockGroupRepository.Setup(repo => repo.GetByIdAsync(groupId)).ReturnsAsync((Group)null);

            // Act
            Func<Task> act = async () => await _groupService.GetGroupMembersAsync(groupId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"Group with ID {groupId} not found.");
        }
    }
}
