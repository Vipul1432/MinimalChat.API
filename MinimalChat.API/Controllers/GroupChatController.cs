using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using MinmalChat.Data.Services;

namespace MinimalChat.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class GroupChatController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupChatController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost("create-group")]
        public async Task<IActionResult> CreateGroup([FromBody] GroupDto groupDto)
        {
            try
            {
                // Check model validation
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<Group>
                    {
                        Message = "Invalid model data",
                        Data = null,
                        StatusCode = 400
                    });
                }

                var addedGroup = await _groupService.CreateGroupAsync(groupDto);
                return Ok(new ApiResponse<Group>
                {
                    Message = "Group created successfully",
                    Data = new Group
                    {
                        Id = addedGroup.Id,
                        Name = addedGroup.Name,
                        Members = addedGroup.Members,
                    },
                    StatusCode = 200
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<Group>
                {
                    Message = "An error occurred while creating the group.",
                    Data = null,
                    StatusCode = 500
                });
            }
        }
    }
}
