using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using MinmalChat.Data.Services;
using System.Net;
using System.Security.Claims;

namespace MinimalChat.API.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class GroupChatController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupChatController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        #region Create Group

        /// <summary>
        /// Handles HTTP POST requests to create a new group.
        /// </summary>
        /// <param name="currentUser">The unique identifier of the current user.</param>
        /// <param name="groupDto">Data transfer object containing group information.</param>
        /// <returns>
        ///   200 OK if the group is created successfully, along with group details.
        ///   400 Bad Request if the model data is invalid.
        ///   500 Internal Server Error if an error occurs during group creation.
        /// </returns>

        [HttpPost("create-group")]
        public async Task<IActionResult> CreateGroupAsync([FromBody] GroupDto groupDto)
        {
            try
            {
                // Check model validation
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Message = "Invalid model data",
                        Data = null,
                        StatusCode = HttpStatusCode.BadRequest
                    });
                }

                // Get the current user's ID from the claims
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var addedGroup = await _groupService.CreateGroupAsync(currentUserId, groupDto);
                return Ok(new ApiResponse<ResponseGroupDto>
                {
                    Message = "Group created successfully",
                    Data = addedGroup,
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

        #endregion Create Group

        #region Add Members to Group

        /// <summary>
        /// Handles HTTP POST requests to add members to a specified group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the target group.</param>
        /// <param name="currentUserId">The unique identifier of the current user initiating the action.</param>
        /// <param name="memberIds">List of unique identifiers of members to be added to the group.</param>
        /// <returns>
        ///   200 OK if members are added successfully, along with a success message.
        ///   500 Internal Server Error if an error occurs during the operation, with details in the response.
        /// </returns>
        [HttpPost("{groupId}/add-member")]
        public async Task<IActionResult> AddMemberToGroupAsync(Guid groupId, [FromBody] AddGroupMemberDto addGroupMemberDto)
        {
            try
            {
                // Get the current user's ID from the claims
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var result = await _groupService.AddMemberToGroupAsync(groupId, currentUserId, addGroupMemberDto);
                 
                return Ok(new ApiResponse<string>
                {
                    Message = result,
                    Data = null,
                    StatusCode = HttpStatusCode.OK,
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

        #endregion Add Members to Group

        #region Remove Member from Group

        /// <summary>
        /// Handles HTTP POST requests to remove a member from a specified group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the target group.</param>
        /// <param name="currentUserId">The unique identifier of the current user initiating the action.</param>
        /// <param name="memberId">The unique identifier of the member to be removed from the group.</param>
        /// <returns>
        ///   200 OK if the member is removed successfully, along with a success message.
        ///   500 Internal Server Error if an error occurs during the operation, with details in the response.
        /// </returns>
        [HttpPost("{groupId}/remove-member")]
        public async Task<IActionResult> RemoveMemberFromGroupAsync(Guid groupId, [FromQuery] Guid memberId)
        {
            try
            {
                // Get the current user's ID from the claims
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var result = await _groupService.RemoveMemberFromGroupAsync(groupId, currentUserId, memberId);

                return Ok(new ApiResponse<string>
                {
                    Message = result,
                    Data = null,
                    StatusCode = HttpStatusCode.OK,
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

        #endregion Remove Member from Group

        #region Edit Group name

        /// <summary>
        /// Updates the name of a group with the specified ID.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group to edit.</param>
        /// <param name="newName">The new name to set for the group.</param>
        /// <returns>
        /// An IActionResult representing the result of the operation.
        /// If successful, returns a 200 OK response with a message indicating success.
        /// If the group is not found, returns a 404 Not Found response.
        /// If an error occurs during the operation, returns a 500 Internal Server Error response.
        /// </returns>
        [HttpPut("{groupId}/edit-group-name")]
        public async Task<IActionResult> EditGroupNameAsync(Guid groupId, [FromQuery] string newName)
        {
            try
            {
                var updatedGroupResult = await _groupService.EditGroupNameAsync(groupId, newName);

                return Ok(new ApiResponse<string>
                {
                    Message = updatedGroupResult,
                    Data = null,
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

        #endregion Edit Group name

        #region Meke group memeber to admin

        /// <summary>
        /// Makes a member an admin of the specified group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group.</param>
        /// <param name="memberId">The unique identifier of the member to make an admin.</param>
        /// <returns>
        /// A response indicating the outcome of the operation:
        /// - If successful, a 200 OK response with a success message.
        /// - If the operation fails, a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPut("make-member-admin")]
        public async Task<IActionResult> MakeMemberAdminAsync([FromQuery] Guid groupId, [FromQuery] Guid memberId)
        {
            try
            {
                // Get the current user's ID from the claims
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var result = await _groupService.MakeMemberAdminAsync(groupId, memberId, currentUserId);

                return Ok(new ApiResponse<string>
                {
                    Message = result,
                    Data = null,
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

        #endregion Make group member to admin

        #region Delete Group

        /// <summary>
        /// Deletes a group if the current user is an admin.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group to delete.</param>
        /// <param name="currentUser">The unique identifier of the current user.</param>
        /// <returns>
        /// An HTTP response indicating the result of the deletion operation. 
        /// Returns a 200 OK response with a message if the group is successfully deleted, 
        /// or a 500 Internal Server Error response if an error occurs during deletion.
        /// </returns>
        [HttpDelete("{groupId}/delete-group")]
        public async Task<IActionResult> DeleteGroupAsync(Guid groupId)
        {
            try
            {
                // Get the current user's ID from the claims
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var deleteGroupResult = await _groupService.DeleteGroupAsync(groupId, currentUserId);

                return Ok(new ApiResponse<string>
                {
                    Message = deleteGroupResult,
                    Data = null,
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

        #endregion Delete Group
    }
}
