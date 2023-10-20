﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Server;
using MinimalChat.API.Hubs;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using MinmalChat.Data.Services;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace MinimalChat.API.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly AppSettings _applicationSettings;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public MessagesController(IMessageService messageService, IUserService userService, IHubContext<ChatHub> hubContext, IMapper mapper, IOptions<AppSettings> applicationSettings)
        {
            _messageService = messageService;
            _userService = userService;
            _hubContext = hubContext;
            _mapper = mapper;
            _applicationSettings = applicationSettings.Value;
        }

        #region Send Message to users

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

                Message message;
                if (!IsUserExist)
                {
                    Guid groupId = Guid.Parse(model.ReceiverId);
                    message = _mapper.Map<Message>(model);
                    message.SenderId = currentUserId;
                    message.GroupId = groupId;
                }
                else
                {
                    message = _mapper.Map<Message>(model);
                    message.SenderId = currentUserId;
                }
                message.Timestamp = DateTime.Now;
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

        #endregion Send Message to 

        #region Edit message send by current user

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
                if (!ModelState.IsValid)
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

                // Broadcast the message via SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", currentUserId, model.Content);

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

        #endregion Edit message send by current user

        #region Delete message send by current user

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

                // Broadcast the message via SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", messageId, currentUserId);

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

        #endregion Delete message send by current user

        #region Retrieve conversation history with specific user

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
                var receiverUserId = await _userService.GetUserByIdAsync(queryParameters.UserId!);
                ConversationHistoryDto conversationHistoryDto;
                GroupMessageDto? messages;
                if (!receiverUserId)
                {
                    conversationHistoryDto = new ConversationHistoryDto
                    {
                        GroupId = Guid.Parse(queryParameters.UserId!),
                    };
                    messages = await _messageService.GetConversationHistoryAsync(conversationHistoryDto, currentUserId);
                }
                else
                {
                    // Retrieve the conversation history
                    messages = await _messageService.GetConversationHistoryAsync(queryParameters, currentUserId);
                }
                // Map group messages
                List<GroupMemberDto> groupMemberDtos = null;
                if (messages.Members != null)
                {
                    groupMemberDtos = messages.Members.ToList().ConvertAll(member =>
                    {
                        var groupMemberDto = _mapper.Map<GroupMemberDto>(member);
                        groupMemberDto.UserName = _userService.GetUserNameByIdAsync(member.UserId).Result;
                        return groupMemberDto;
                    });
                }

                if (messages == null || messages.Messages.Count <= 0)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Message = "No more conversation found.",
                        Data = groupMemberDtos ?? null,
                        StatusCode = 200
                    });
                }

                // Map messages
                var messageDtos = messages.Messages.Select(message =>
                {
                    var messageDto = _mapper.Map<GetMessagesDto>(message);
                    messageDto.Users = groupMemberDtos;
                    return messageDto;
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
                    Message = ex.StackTrace,
                    Data = null,
                    StatusCode = 500
                });
            }
        }

        #endregion Retrieve conversation history with specific user

        #region Retrieve message based on input

        /// <summary>
        /// Searches conversations for messages containing a provided keyword.
        /// </summary>
        /// <param name="query">The keyword to search for within conversations.</param>
        /// <param name="receiverId">The ID of the receiver user for filtering conversations.</param>
        /// <returns>
        /// An IActionResult representing the result of the conversation search operation.
        /// If successful, returns a list of messages matching the keyword.
        /// </returns>
        [HttpGet("conversation/search")]
        public async Task<IActionResult> SearchConversations([FromQuery] string query)
        {
            try
            {
                // Get the current user's ID from the JWT token
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new ApiResponse<Message>
                    {
                        Message = "Unauthorized access",
                        Data = null,
                        StatusCode = 401
                    });
                }

                // Perform the conversation search
                var conversations = await _messageService.SearchConversationsAsync(query, currentUserId);

                if (conversations == null || conversations.Count <= 0)
                {
                    return BadRequest(new ApiResponse<Message>
                    {
                        Message = "No message is found with this keyword",
                        Data = null,
                        StatusCode = 200
                    });
                }
                // Map Mesage to MessageDto
                var messageDtos = _mapper.Map<List<MessageDto>>(conversations);
                return Ok(new
                {
                    messages = messageDtos
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

        #endregion Retrieve message based on input

        #region Send Files

        [HttpPost("messages/upload/{receiverId}")]
        public async Task<IActionResult> UploadFile(string receiverId, IFormFile file)
        {
            try
            {
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file selected or the file is empty.");
                }

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    var fileUploadDto = new FileUploadDto
                    {
                        SenderId = senderId,
                        ReceiverId = receiverId,
                        FileData = stream.ToArray(),
                        FileName = file.FileName,
                        UploadDirectory = _applicationSettings.UploadDirectory
                    };
                    var uploadedFilePath = await _messageService.UploadFileAsync(fileUploadDto);

                    if (!string.IsNullOrEmpty(uploadedFilePath))
                    {
                        return Ok(new
                        {
                            Message = "File uploaded successfully",
                            FilePath = uploadedFilePath
                        });
                    }
                    else
                    {
                        return BadRequest("File upload failed.");
                    }
                }
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



        #endregion Send Files
    }
}
