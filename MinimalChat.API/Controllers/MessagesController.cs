using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Server;
using MinimalChat.API.Hubs;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using MinmalChat.Data.Services;
using System.Globalization;
using System.Net;
using System.Net.Mime;
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
        public async Task<IActionResult> SendMessageAsync([FromBody] MessageDto model)
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
                    message.ReceiverId = null;
                }
                else
                {
                    message = _mapper.Map<Message>(model);
                    message.SenderId = currentUserId;
                }
                message.Timestamp = DateTime.Now;
                var result = await _messageService.SendMessageAsync(message);

                // Broadcast the message via SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", message.ReceiverId, message.Content);


                return Ok(new ApiResponse<GetMessagesDto>
                {
                    Message = "Message sent successfully",
                    Data = null,
                    StatusCode = HttpStatusCode.OK,
                });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new ApiResponse<Object>
                {
                    Message = "An error occurred! Please try again.",
                    Data = null,
                    StatusCode = HttpStatusCode.InternalServerError
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
        public async Task<IActionResult> EditMessageAsync(int messageId, [FromBody] EditMessageDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<GetMessagesDto>
                    {
                        Message = "Message editing failed due to validation errors.",
                        Data = null,
                        StatusCode = HttpStatusCode.BadRequest
                    });
                }
                // Get the current user's ID from the JWT token
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Edit the message in the repository
                var edited = await _messageService.EditMessageAsync(messageId, model.Content, currentUserId!);

                if(edited.StatusCode == HttpStatusCode.OK)
                {
                    string ReceiverId = edited.Data.ReceiverId;

                    // Broadcast the message via SignalR
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", ReceiverId, model.Content);
                }
                
                return StatusCode((int)edited.StatusCode, new ApiResponse<GetMessagesDto>
                {
                    Message = edited.Message,
                    Data = null,
                    StatusCode = edited.StatusCode
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
        public async Task<IActionResult> DeleteMessageAsync(int messageId)
        {
            try
            {
                // Get the current user's ID from the JWT token
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Delete the message in the repository
                var deleted = await _messageService.DeleteMessageAsync(messageId, currentUserId!);

                if(deleted.StatusCode == HttpStatusCode.OK)
                {
                    string ReceiverId = deleted.Data.ReceiverId;

                    // Broadcast the message via SignalR
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", ReceiverId, messageId);
                }

                return StatusCode((int)deleted.StatusCode, new ApiResponse<GetMessagesDto>
                {
                    Message = deleted.Message,
                    Data = null,
                    StatusCode = deleted.StatusCode
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
        public async Task<IActionResult> GetConversationHistoryAsync([FromQuery] ConversationHistoryDto queryParameters)
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
                        StatusCode = HttpStatusCode.BadRequest
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
                        StatusCode = HttpStatusCode.Unauthorized
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
                        StatusCode = HttpStatusCode.OK
                    });
                }

                // Map messages
                var messageDtos = messages.Messages.Select(message =>
                {
                    var messageDto = _mapper.Map<GetMessagesDto>(message);
                    if (string.IsNullOrEmpty(messageDto.Content))
                    {
                        string fileName = message.FilePath;
                        var filePath = Path.Combine(_applicationSettings.UploadDirectory, fileName);
                        if (System.IO.File.Exists(filePath))
                        {
                            string pattern = @"^[\w-]+_(.*)$";

                            Match match = Regex.Match(fileName, pattern);

                            if (match.Success)
                            {
                                messageDto.FileName = match.Groups[1].Value;
                            }
                            messageDto.FilePath = message.FilePath;
                        }

                    }
                    messageDto.Users = groupMemberDtos;
                    return messageDto;
                }).ToList();

                return Ok(new ApiResponse<List<GetMessagesDto>>
                {
                    Message = "Conversation history retrieved successfully",
                    Data = messageDtos,
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
        public async Task<IActionResult> SearchConversationsAsync([FromQuery] string query)
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
                        StatusCode = HttpStatusCode.Unauthorized
                    });
                }

                // Perform the conversation search
                var conversations = await _messageService.SearchConversationsAsync(query, currentUserId);

                if (conversations == null || conversations.Count <= 0)
                {
                    return NotFound(new ApiResponse<Message>
                    {
                        Message = "No message is found with this keyword",
                        Data = null,
                        StatusCode = HttpStatusCode.NotFound
                    });
                }
                // Map Mesage to MessageDto
                var messageDtos = _mapper.Map<List<ResponseMessageDto>>(conversations);
                return Ok(new
                {
                    messages = messageDtos
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

        #endregion Retrieve message based on input

        #region Send Files

        /// <summary>
        /// Handles the upload of a file for messaging purposes.
        /// </summary>
        /// <param name="receiverId">The recipient's user ID or group ID.</param>
        /// <param name="file">The file to be uploaded.</param>
        /// <returns>Returns an IActionResult indicating the result of the file upload operation, including success or error messages.
        /// </returns>
        /// <remarks>
        /// This action method receives a file, checks if it's valid, and processes it for either an individual recipient or a group.
        /// It uses SignalR to broadcast the uploaded file's information to all connected clients.
        /// </remarks>
        [HttpPost("messages/upload/{receiverId}")]
        public async Task<IActionResult> UploadFileAsync(string receiverId, IFormFile file)
        {
            try
            {
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file selected or the file is empty.");
                }

                bool IsUserExist = await _userService.GetUserByIdAsync(receiverId);

                FileUploadDto fileUploadDto;
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);

                    if (!IsUserExist)
                    {
                        fileUploadDto = new FileUploadDto
                        {
                            SenderId = senderId,
                            GroupId = Guid.Parse(receiverId),
                            FileData = stream.ToArray(),
                            FileName = file.FileName,
                            UploadDirectory = _applicationSettings.UploadDirectory
                        };
                    }
                    else
                    {
                        fileUploadDto = new FileUploadDto
                        {
                            SenderId = senderId,
                            ReceiverId = receiverId,
                            FileData = stream.ToArray(),
                            FileName = file.FileName,
                            UploadDirectory = _applicationSettings.UploadDirectory
                        };
                    }
                   
                    var uploadedFilePath = await _messageService.UploadFileAsync(fileUploadDto);

                    // Broadcast the message via SignalR
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", fileUploadDto.ReceiverId, file.FileName);

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

        #endregion Send Files

        #region Download Files

        /// <summary>
        /// Retrieves and serves a file for download by its associated message ID.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message containing the file to be downloaded.</param>
        /// <returns>Returns an IActionResult containing the requested file for download, if it exists, or returns a 'File not found' response.
        /// </returns>
        /// <remarks>
        /// This action method retrieves a file by its associated message ID, and if the file exists, it serves it for download.
        /// It sets the appropriate content type and response headers to trigger a download prompt in the client's browser.
        /// </remarks>
        [HttpGet("download/{messageId}")]
        public async Task<IActionResult> DownloadFileAsync(int messageId)
        {
            var message = await _messageService.GetMessageByIdAsync(messageId);
            string storedfileName = message.FilePath!;
            var filePath = Path.Combine(_applicationSettings.UploadDirectory, storedfileName);

            if (System.IO.File.Exists(filePath))
            {
                string contentType = GetContentType(filePath);
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                string pattern = @"^[\w-]+_(.*)$";

                Match match = Regex.Match(storedfileName, pattern);
                var fileName = match.Groups[1].Value;
                Response.Headers.Add("Content-Disposition", new ContentDisposition
                {
                    FileName = fileName,
                    //It open the file in the browser
                    Inline = true, 
                }.ToString());

                return File(fileBytes, contentType, fileName);
            }
            else
            {
                return NotFound("File not found");
            }
        }

        /// <summary>
        /// Determines and returns the content type (MIME type) of a file based on its file extension.
        /// </summary>
        /// <param name="filePath">The path to the file for which the content type is to be determined.</param>
        /// <returns>
        /// A string representing the content type (MIME type) of the file. If the content type cannot be determined, it defaults to "application/octet-stream."
        /// </returns>
        /// <remarks>
        /// This method uses a `FileExtensionContentTypeProvider` to guess the content type of a file based on its extension.
        /// If the content type cannot be determined, it defaults to a generic "application/octet-stream" type.
        /// </remarks>
        private string GetContentType(string filePath)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (provider.TryGetContentType(filePath, out var guessedType))
            {
                return guessedType;
            }

            return "application/octet-stream";
        }

        #endregion Download Files
    }
}
