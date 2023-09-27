using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SqlServer.Server;
using MinimalChat.API.Hubs;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using MinmalChat.Data.Services;
using System.Globalization;
using System.Security.Claims;

namespace MinimalChat.API.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IUserService _userService;

        public MessagesController(IMessageService messageService, IUserService userService, IHubContext<ChatHub> hubContext)
        {
            _messageService = messageService;
            _userService = userService;
            _hubContext = hubContext;
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

                var result = await _messageService.SendMessageAsync(message);

                // Broadcast the message via SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", message.SenderId, message.Content);


                return Ok(new ApiResponse<GetMessagesDto>
                {
                    Message = "Message sent successfully",
                    Data = null,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<GetMessagesDto>
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
                    return BadRequest(new ApiResponse<GetMessagesDto>
                    {
                        Message = "Message editing failed due to validation errors.",
                        Data = null,
                        StatusCode = 404
                    });
                }
                // Get the current user's ID from the JWT token
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Edit the message in the repository
                var edited = await _messageService.EditMessageAsync(messageId, model.Content, currentUserId!);

                return StatusCode(edited.StatusCode, new ApiResponse<GetMessagesDto>
                {
                    Message = edited.Message,
                    Data = null,
                    StatusCode = edited.StatusCode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<GetMessagesDto>
                {
                    Message = ex.Message,
                    Data = null,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Deletes a message with the specified ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to delete.</param>
        /// <returns>
        /// 200 OK if the message is deleted successfully.
        /// 401 Unauthorized if the user is not authorized to delete the message.
        /// 404 Not Found if the message to delete is not found.
        /// 500 Internal Server Error if an unexpected error occurs.
        /// </returns>
        [HttpDelete("messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            try
            {
                // Get the current user's ID from the JWT token
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Delete the message in the repository
                var deleted = await _messageService.DeleteMessageAsync(messageId, currentUserId!);

                return StatusCode(deleted.StatusCode, new ApiResponse<GetMessagesDto>
                {
                    Message = deleted.Message,
                    Data = null,
                    StatusCode = deleted.StatusCode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<GetMessagesDto>
                {
                    Message = ex.Message,
                    Data = null,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Retrieves the conversation history between two users based on specified query parameters.
        /// </summary>
        /// <param name="queryParameters">The query parameters specifying user IDs, timestamp, count, and sort order.</param>
        /// <returns>
        /// 200 OK if the conversation history is retrieved successfully.
        /// 400 Bad Request if validation errors occur or if the conversation is not found.
        /// 401 Unauthorized if the user is not authorized to access the conversation.
        /// 500 Internal Server Error if an unexpected error occurs.
        /// </returns>
        [HttpGet("messages")]
        public async Task<IActionResult> GetConversationHistory([FromQuery] ConversationHistoryDto queryParameters)
        {
            try
            {
                // check the model Validations
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<GetMessagesDto>
                    {
                        Message = "Invalid request parameters.",
                        Data = null,
                        StatusCode = 400
                    });
                }

                // Get the current user's ID from the JWT token
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new ApiResponse<GetMessagesDto>
                    {
                        Message = "Unauthorized access",
                        Data = null,
                        StatusCode = 401
                    });
                }

                // Check receiverUSer Id exist or not 
                var receiverUserId = await _userService.GetUserByIdAsync(queryParameters.UserId.ToString());
                if (!receiverUserId)
                {
                    return BadRequest(new ApiResponse<GetMessagesDto>
                    {
                        Message = "User not found.",
                        Data = null,
                        StatusCode = 400
                    });
                }

                // Retrieve the conversation history
                var messages = await _messageService.GetConversationHistoryAsync(queryParameters, currentUserId);

                if (messages == null || messages.Count <= 0)
                {
                    return Ok(new ApiResponse<GetMessagesDto>
                    {
                        Message = "No more conversation found.",
                        Data = null,
                        StatusCode = 400
                    });
                }
                // Map messages
                var messageDtos = messages.Select(message => new GetMessagesDto
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    ReceiverId = message.ReceiverId,
                    Content = message.Content,
                    Timestamp = message.Timestamp
                }).ToList();

                return Ok(new ApiResponse<List<GetMessagesDto>>
                {
                    Message = "Conversation history retrieved successfully",
                    Data = messageDtos,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<GetMessagesDto>
                {
                    Message = ex.Message,
                    Data = null,
                    StatusCode = 500
                });
            }
        }
    }
}
