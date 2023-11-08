using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Context;
using MinmalChat.Data.Helpers;
using System.Net;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace MinmalChat.Data.Services
{
    public class MessageService : IMessageService
    {
        private readonly IRepository<Message> _messageRepository;
        private readonly MinimalChatDbContext _context;
        private readonly IMapper _mapper;

        public MessageService(IRepository<Message> messageRepository, MinimalChatDbContext context, IMapper mapper)
        {
            _messageRepository = messageRepository;
            _context = context;
            _mapper = mapper;
        }

        /// <summary>
        /// Asynchronously sends a chat message.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <returns>A task representing the result of the send operation.</returns>
        public async Task<bool> SendMessageAsync(Message message)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                return false;
            }
            return await _messageRepository.AddAsync(message) != null;
        }

        /// <summary>
        /// Asynchronously edits a chat message with the specified ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to edit.</param>
        /// <param name="updatedContent">The updated content for the message.</param>
        /// <param name="currentUserId">The ID of the current user editing the message.</param>
        /// <returns>An ApiResponse containing the result of the edit operation.</returns>
        public async Task<ApiResponse<ResponseMessageDto>> EditMessageAsync(int messageId, string updatedContent, string currentUserId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId);

            if (message == null)
            {
                return new ApiResponse<ResponseMessageDto>
                {
                    Message = "Message not found.",
                    Data = null,
                    StatusCode = HttpStatusCode.NotFound,
                };
            }

            // Check if the user is the sender of the message
            if (message.SenderId != currentUserId)
            {
                return new ApiResponse<ResponseMessageDto>
                {
                    Message = "Unauthorized access.",
                    Data = null,
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            // Additional logic for editing the message
            message.Content = updatedContent;
            await _messageRepository.UpdateAsync(message);

            return new ApiResponse<ResponseMessageDto>
            {
                Message = "Message edited successfully",
                Data = _mapper.Map<Message, ResponseMessageDto>(message),
                StatusCode = HttpStatusCode.OK
            };
        }

        /// <summary>
        /// Asynchronously deletes a chat message with the specified ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to delete.</param>
        /// <param name="currentUserId">The ID of the current user deleting the message.</param>
        /// <returns>An ApiResponse containing the result of the delete operation.</returns>
        public async Task<ApiResponse<ResponseMessageDto>> DeleteMessageAsync(int messageId, string currentUserId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId);

            if (message == null)
            {
                return new ApiResponse<ResponseMessageDto>
                {
                    Message = "Message not found",
                    Data = null,
                    StatusCode = HttpStatusCode.NotFound
                };
            }

            // Check if the user is the sender of the message
            if (message.SenderId != currentUserId)
            {
                return new ApiResponse<ResponseMessageDto>
                {
                    Message = "Unauthorized access",
                    Data = null,
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            await _messageRepository.DeleteAsync(messageId);

            return new ApiResponse<ResponseMessageDto>
            {
                Message = "Message deleted successfully",
                Data = _mapper.Map<Message, ResponseMessageDto>(message),
                StatusCode = HttpStatusCode.OK
            };
        }

        /// <summary>
        /// Retrieves a conversation history between two users based on specified query parameters.
        /// </summary>
        /// <param name="queryParameters">The query parameters specifying user IDs, timestamp, count, and sort order.</param>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <returns>A list of messages representing the conversation history.</returns>
        public async Task<GroupMessageDto?> GetConversationHistoryAsync(ConversationHistoryDto queryParameters, string currentUserId)
        {
            IQueryable<Message> query;
            List<GroupMember?> groupUsers = null;
            if (queryParameters.UserId != null)
            {
                query = _context.Messages.Where(m => (m.GroupId == null && m.FilePath == null &&
                                            (m.SenderId == currentUserId && m.ReceiverId == queryParameters.UserId) ||
                                            (m.SenderId == queryParameters.UserId && m.ReceiverId == currentUserId)) ||
                                            (m.Content == null && m.GroupId == null &&
                                            (m.SenderId == currentUserId && m.ReceiverId == queryParameters.UserId) ||
                                            (m.SenderId == queryParameters.UserId && m.ReceiverId == currentUserId)));
            }
            else
            {
                DateTime? chatHistoryTime = _context.GroupMembers.Where(m =>m.GroupId == queryParameters.GroupId && m.UserId == currentUserId).Select(ct =>ct.ChatHistoryTime).FirstOrDefault();
                query = _context.Messages.Where(m => (m.ReceiverId == null && m.Content != null && m.GroupId == queryParameters.GroupId && m.Timestamp >= chatHistoryTime) ||
                                          (m.ReceiverId == null && m.Content == null && m.GroupId == queryParameters.GroupId && m.FilePath != null && m.Timestamp >= chatHistoryTime));
                groupUsers = await _context.GroupMembers.Where(gm => gm.GroupId == queryParameters.GroupId).ToListAsync();
            }

            query = queryParameters.SortOrder == MinimalChat.Domain.Enum.SortOrder.desc
                                                                     ? query.OrderBy(m => m.Timestamp)
                                                                     : query.OrderByDescending(m => m.Timestamp);

            //Limit the number of messages retrieved based on the specified count
            if (queryParameters.Count > 0)
            {
                query = query.Take(queryParameters.Count);
            }

            //Sort the messages based on the specified sort order
            query = queryParameters.SortOrder == MinimalChat.Domain.Enum.SortOrder.asc
                                                                    ? query.OrderBy(m => m.Timestamp)
                                                                    : query.OrderByDescending(m => m.Timestamp);

            // Execute the query and return the conversation history
            List<Message?> messages =  await query.ToListAsync();
            GroupMessageDto groupMessages = new GroupMessageDto()
            {
                Messages = _mapper.Map<List<ResponseMessageDto>>(messages)!,
                Members = _mapper.Map<List<GroupMemberDto>>(groupUsers)!,
            };
            return groupMessages;
        }

        /// <summary>
        /// Searches conversations for messages containing a provided keyword.
        /// </summary>
        /// <param name="query">The keyword to search for within conversations.</param>
        /// <param name="receiverId">The ID of the receiver user for filtering conversations.</param>
        /// <param name="currentUserId">The ID of the current user initiating the search.</param>
        /// <returns>
        /// A list of messages matching the keyword within conversations.
        /// </returns>
        public async Task<List<ResponseMessageDto>> SearchConversationsAsync(string query, string currentUserId)
        {
            var messages = await _context.Messages.Where(m => (m.SenderId == currentUserId || m.ReceiverId == currentUserId) && m.Content.Contains(query)).ToListAsync();
            return _mapper.Map<List<ResponseMessageDto>>(messages);
        }

        /// <summary>
        /// Asynchronously uploads a file based on the provided information in the 'fileUploadDto.'
        /// </summary>
        /// <param name="fileUploadDto">Data transfer object containing file information and upload details.</param>
        /// <returns>A task that represents the asynchronous operation, returning a string representing the uploaded file's path if successful.</returns>
        /// <remarks>
        /// This method is used to asynchronously upload a file and returns the file's path upon successful completion. The operation is based on the information provided in the 'fileUploadDto.'
        /// </remarks>
        public async Task<string> UploadFileAsync(FileUploadDto fileUploadDto)
        {
            try
            {
                if (fileUploadDto.FileData == null || fileUploadDto.FileData.Length == 0)
                {
                    throw new ArgumentException("File data is empty or null.");
                }

                Directory.CreateDirectory(fileUploadDto.UploadDirectory);

                string uniqueFileName = Guid.NewGuid() + "_" + fileUploadDto.FileName;

                string filePath = Path.Combine(fileUploadDto.UploadDirectory, uniqueFileName);

                await File.WriteAllBytesAsync(filePath, fileUploadDto.FileData);

                // Store the file name in the database
                var fileEntity = new Message
                {
                    FilePath = uniqueFileName,
                    SenderId = fileUploadDto.SenderId, 
                    ReceiverId = fileUploadDto.ReceiverId,
                    GroupId = fileUploadDto.GroupId,
                    Timestamp = DateTime.Now,
                };

                // Assuming _dbContext is your database context
                _context.Messages.Add(fileEntity);
                await _context.SaveChangesAsync();

                return filePath;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Asynchronously retrieves a message by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the message to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation, returning a 'Message' object when the message is successfully retrieved.</returns>
        /// <remarks>
        /// This method is used to asynchronously fetch a message by its unique identifier. It returns a 'Message' object when the specified message is found and retrieved.
        /// </remarks>
        public async Task<ResponseMessageDto> GetMessageByIdAsync(int id)
        {
            try
            {
               Message message = await _context.Messages.Where(m => m.Id == id).FirstOrDefaultAsync();
               return _mapper.Map<Message, ResponseMessageDto>(message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }

}
