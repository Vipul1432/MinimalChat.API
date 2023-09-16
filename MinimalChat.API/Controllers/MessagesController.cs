using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using MinmalChat.Data.Services;
using System.Security.Claims;

namespace MinimalChat.API.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserService _userService;

        public MessagesController(IMessageRepository messageRepository, IUserService userService)
        {
            _messageRepository = messageRepository;
            _userService = userService;
        }

        /// <summary>
        /// Sends a message from the current user to another user identified by the receiver's ID.
        /// </summary>
        /// <param name="model">The message data to be sent.</param>
        /// <returns>An IActionResult representing the result of the message sending operation.</returns>
        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] MessageDto model)
        {
            try
            {
                // Get the current user's ID from the claims
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                bool IsUserExist = await _userService.GetUserByIdAsync(model.ReceiverId);

                if(!IsUserExist)
                {
                    return BadRequest(new ApiResponse<LoginDto>
                    {
                        Message = "Message not sent receiver User not exist!",
                        Data = null,
                        StatusCode = 400
                    });
                }

                var message = new Message
                {
                    SenderId = currentUserId,
                    ReceiverId = model.ReceiverId,
                    Content = model.Content,
                    Timestamp = DateTime.Now
                };

                var result = await _messageRepository.SendMessageAsync(message);

                return Ok(new ApiResponse<Message>
                {
                    Message = "Message sent successfully",
                    Data = null,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<Message>
                {
                    Message = ex.Message,
                    Data = null,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Edits a message with the specified ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to edit.</param>
        /// <param name="model">The model containing the updated message content.</param>
        /// <returns>
        /// 200 OK if the message is edited successfully.
        /// 400 Bad Request if validation errors occur.
        /// 401 Unauthorized if the user is not authorized to edit the message.
        /// 404 Not Found if the message to edit is not found.
        /// 500 Internal Server Error if an unexpected error occurs.
        /// </returns>
        [HttpPut("messages/{messageId}")]
        public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessageDto model)
        {
            try
            {
                if(!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<Message>
                    {
                        Message = "Message editing failed due to validation errors.",
                        Data = null,
                        StatusCode = 404
                    });
                }
                // Get the current user's ID from the JWT token
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Edit the message in the repository
                var edited = await _messageRepository.EditMessageAsync(messageId, model.Content, currentUserId!);

                return StatusCode(edited.StatusCode, new ApiResponse<Message>
                {
                    Message = edited.Message,
                    Data = null,
                    StatusCode = edited.StatusCode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<Message>
                {
                    Message = ex.Message,
                    Data = null,
                    StatusCode = 500
                });
            }
        }

    }
}
