using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Context;
using MinmalChat.Data.Helpers;

namespace MinmalChat.Data.Services
{
    public class MessageService : IMessageService
    {
        private readonly IRepository<Message> _messageRepository;
        private readonly MinimalChatDbContext _context;

        public MessageService(IRepository<Message> messageRepository, MinimalChatDbContext context)
        {
            _messageRepository = messageRepository;
            _context = context;
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
        public async Task<ApiResponse<Message>> EditMessageAsync(int messageId, string updatedContent, string currentUserId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId);

            if (message == null)
            {
                return new ApiResponse<Message>
                {
                    Message = "Message not found.",
                    Data = null,
                    StatusCode = 404
                };
            }

            // Check if the user is the sender of the message
            if (message.SenderId != currentUserId)
            {
                return new ApiResponse<Message>
                {
                    Message = "Unauthorized access.",
                    Data = null,
                    StatusCode = 401
                };
            }

            // Additional logic for editing the message
            message.Content = updatedContent;
            await _messageRepository.UpdateAsync(message);

            return new ApiResponse<Message>
            {
                Message = "Message edited successfully",
                Data = null,
                StatusCode = 200
            };
        }

        /// <summary>
        /// Asynchronously deletes a chat message with the specified ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to delete.</param>
        /// <param name="currentUserId">The ID of the current user deleting the message.</param>
        /// <returns>An ApiResponse containing the result of the delete operation.</returns>
        public async Task<ApiResponse<Message>> DeleteMessageAsync(int messageId, string currentUserId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId);

            if (message == null)
            {
                return new ApiResponse<Message>
                {
                    Message = "Message not found",
                    Data = null,
                    StatusCode = 404
                };
            }

            // Check if the user is the sender of the message
            if (message.SenderId != currentUserId)
            {
                return new ApiResponse<Message>
                {
                    Message = "Unauthorized access",
                    Data = null,
                    StatusCode = 401
                };
            }

            await _messageRepository.DeleteAsync(messageId);

            return new ApiResponse<Message>
            {
                Message = "Message deleted successfully",
                Data = null,
                StatusCode = 200
            };
        }

        /// <summary>
        /// Retrieves a conversation history between two users based on specified query parameters.
        /// </summary>
        /// <param name="queryParameters">The query parameters specifying user IDs, timestamp, count, and sort order.</param>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <returns>A list of messages representing the conversation history.</returns>
        public async Task<List<Message?>> GetConversationHistoryAsync(ConversationHistoryDto queryParameters, string currentUserId)
        {
            var query = _context.Messages.Where(m =>
                                            (m.SenderId == currentUserId && m.ReceiverId == queryParameters.UserId.ToString()) ||
                                            (m.SenderId == queryParameters.UserId.ToString() && m.ReceiverId == currentUserId))
                                         .Where(m => m.Timestamp < queryParameters.Before);

            query = query.OrderByDescending(m => m.Timestamp);

            // Limit the number of messages retrieved based on the specified count
            if (queryParameters.Count > 0)
            {
                query = query.Take(queryParameters.Count);
            }

            // Sort the messages based on the specified sort order
            if (queryParameters.SortOrder == MinimalChat.Domain.Enum.SortOrder.asc)
            {
                query = query.OrderBy(m => m.Timestamp);
            }
            else
            {
                query = query.OrderByDescending(m => m.Timestamp);
            }

            // Execute the query and return the conversation history
            return await query.ToListAsync();
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
        public async Task<List<Message>> SearchConversationsAsync(string query, string currentUserId)
        {
            var filteredConversations = await _context.Messages
                                         .Where(m => (m.SenderId == currentUserId) || (m.ReceiverId == currentUserId))
                                         .Where(m => m.Content.Contains(query))
                                         .Select(m => new Message
                                         {
                                             Id = m.Id,
                                             SenderId = m.SenderId,
                                             ReceiverId = m.ReceiverId,
                                             Content = m.Content,
                                             Timestamp = m.Timestamp
                                         })
                                         .ToListAsync();

            return filteredConversations;
        }
    }

}
