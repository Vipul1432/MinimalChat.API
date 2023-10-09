using Microsoft.EntityFrameworkCore;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Context;
using MinmalChat.Data.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinmalChat.Data.Services
{
    public class GroupService : IGroupService
    {
        private readonly IRepository<Group> _groupRepository;
        private readonly IRepository<GroupMember> _groupMemberRepository;
        private readonly MinimalChatDbContext _context;

        public GroupService(IRepository<Group> groupRepository, IRepository<GroupMember> groupMemberRepository, MinimalChatDbContext context)
        {
            _groupRepository = groupRepository;
            _groupMemberRepository = groupMemberRepository;
            _context = context;
        }

        /// <summary>
        /// Asynchronously creates a new group based on the provided information.
        /// </summary>
        /// <param name="currentUser">The unique identifier of the user creating the group.</param>
        /// <param name="groupDto">Data transfer object containing information about the group.</param>
        /// <returns>
        ///   A task representing the asynchronous operation, with the created Group object.
        /// </returns>
        public async Task<Group> CreateGroupAsync(Guid currentUser, GroupDto groupDto)
        {
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = groupDto.Name,
            };

            if(!groupDto.Members!.Contains(currentUser))
            {
                groupDto.Members.Add(currentUser);
            }

            var addedGroup = await _groupRepository.AddAsync(group);

            if (groupDto.Members != null)
            {
                foreach (var memberId in groupDto.Members)
                {
                    var isAdmin = memberId == currentUser;
                    var groupMember = new GroupMember
                    {
                        GroupId = addedGroup.Id,
                        UserId = memberId.ToString(),
                        IsAdmin = isAdmin,
                    };

                    await _groupMemberRepository.AddAsync(groupMember);
                }
            }

            return addedGroup;
        }

        /// <summary>
        /// Asynchronously adds members to a specified group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the target group.</param>
        /// <param name="currentUserId">The unique identifier of the current user initiating the action.</param>
        /// <param name="memberIds">List of unique identifiers of members to be added to the group.</param>
        /// <returns>
        ///   A task representing the asynchronous operation, with a string result indicating the outcome.
        /// </returns>
        public async Task<string> AddMemberToGroupAsync(Guid groupId, Guid currentUserId, List<Guid> memberIds)
        {
            // Check if the group exists
            var group = await _context.Groups.FirstOrDefaultAsync(grp => grp.Id == groupId);
            if (group == null)
            {
                return "Group not found";
            }

            var groupMember =  await _context.GroupMembers.FirstOrDefaultAsync(grpmem => grpmem.UserId == currentUserId.ToString());

            if (groupMember != null && !groupMember.IsAdmin)
            {
                return "You are not an admin! You can't add members!";
            }

            foreach (var memberId in memberIds)
            {
                // Check if the member already exists in the group
                var memberExists = await _context.GroupMembers.AnyAsync(grpmem => grpmem.GroupId == groupId && grpmem.UserId == memberId.ToString());

                if (memberExists)
                {
                    // Handle the case where the member already exists in the group
                    return $"Member with ID {memberId} already exists in the group";
                }
                var isAdmin = memberId == currentUserId;
                var groupUser = new GroupMember
                {
                    GroupId = groupId,
                    UserId = memberId.ToString(),
                    IsAdmin = isAdmin,
                };

                await _groupMemberRepository.AddAsync(groupUser);
            }
            return "Member Added Successfully!";
        }

        /// <summary>
        /// Asynchronously removes a member from a specified group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the target group.</param>
        /// <param name="currentUserId">The unique identifier of the current user initiating the action.</param>
        /// <param name="memberId">The unique identifier of the member to be removed from the group.</param>
        /// <returns>
        ///   A task representing the asynchronous operation, with a string result indicating the outcome.
        /// </returns>
        public async Task<string> RemoveMemberFromGroupAsync(Guid groupId, Guid currentUserId, Guid memberId)
        {
            // Check if the group exists
            var group = await _context.Groups.FirstOrDefaultAsync(grp => grp.Id == groupId);
            if (group == null)
            {
                return "Group not found";
            }

            var groupMember = await _context.GroupMembers.FirstOrDefaultAsync(grpmem => grpmem.UserId == currentUserId.ToString());

            if (groupMember != null && !groupMember.IsAdmin)
            {
                return "You are not an admin! You can't remove members!";
            }

            // Check if the member exists in the group
            var memberExists = await _context.GroupMembers.FirstOrDefaultAsync(grpmem => grpmem.GroupId == groupId && grpmem.UserId == memberId.ToString());

            if (memberExists == null)
            {
                return $"Member with ID {memberId} not found in the group";
            }

            // Remove the member from the group
            _context.GroupMembers.Remove(memberExists);
            await _context.SaveChangesAsync();

            return "Member Removed Successfully!";
        }

        /// <summary>
        /// Edits the name of a group with the specified ID.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group to edit.</param>
        /// <param name="newName">The new name to set for the group.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result is a string message indicating the outcome of the operation.
        /// If successful, the message will indicate success.
        /// If the group is not found, the message will indicate that the group was not found.
        /// </returns>
        public async Task<string> EditGroupNameAsync(Guid groupId, string newName)
        {
            var group = await _context.Groups.FindAsync(groupId);

            if (group == null)
            {
                return "Group not found";
            }

            group.Name = newName;
            await _context.SaveChangesAsync();

            return "Name updated sucessfully!";
        }

        /// <summary>
        /// Makes a group member an admin of the specified group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group.</param>
        /// <param name="memberId">The unique identifier of the member to make an admin.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result is a string message indicating the outcome of the operation.
        /// If successful, the message will indicate that the member is now an admin.
        /// If the group or member is not found, appropriate error messages will be returned.
        /// </returns>
        public async Task<string> MakeMemberAdminAsync(Guid groupId, Guid memberId)
        {
            var group = await _context.Groups.FindAsync(groupId);

            if (group == null)
            {
                return "Group not found";
            }

            var groupMember = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId && gm.UserId == memberId.ToString())
                .FirstOrDefaultAsync();

            if (groupMember == null)
            {
                return "Member not found in the group";
            }

            if (groupMember == null)
            {
                return "Member not found in the group";
            }

            groupMember.IsAdmin = true;

            await _context.SaveChangesAsync();

            return "Member is now an admin";
        }


    }
}
