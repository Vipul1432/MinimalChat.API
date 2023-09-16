using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Context;
using MinmalChat.Data.Helpers;

namespace MinmalChat.Data.Repository
{
    public class MessageRepository : IMessageRepository
    {
        private readonly MinimalChatDbContext _context;

        public MessageRepository(MinimalChatDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a message to the database and saves changes asynchronously.
        /// </summary>
        /// <param name="message">The message to be added and saved.</param>
        /// <returns>A boolean indicating whether the message was successfully sent and saved.</returns>
        public async Task<bool> SendMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Edits a message with the specified ID asynchronously.
        /// </summary>
        /// <param name="messageId">The ID of the message to edit.</param>
        /// <param name="updatedContent">The updated content for the message.</param>
        /// <param name="currentUserId">The ID of the current user editing the message.</param>
        /// <returns>
        /// A task representing the operation, returning an ApiResponse containing the result:
        /// - Message edited successfully with a status code of 200 if successful.
        /// - Message not found with a status code of 404 if the message to edit is not found.
        /// - Unauthorized access with a status code of 401 if the user is not authorized to edit the message.
        /// </returns>
        public async Task<ApiResponse<Message>> EditMessageAsync(int messageId, string updatedContent, string currentUserId)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);

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

            message.Content = updatedContent;
            await _context.SaveChangesAsync();

            return new ApiResponse<Message>
            {
                Message = "Message edited successfully",
                Data = null,
                StatusCode = 200
            };
        }

        /// <summary>
        /// Deletes a message with the specified message ID if it exists and if the current user is the sender.
        /// </summary>
        /// <param name="messageId">The ID of the message to delete.</param>
        /// <param name="currentUserId">The ID of the current authenticated user.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> indicating the result of the delete operation.</returns>
        public async Task<ApiResponse<Message>> DeleteMessageAsync(int messageId, string currentUserId)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);

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

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return new ApiResponse<Message>
            {
                Message = "Message deleted successfully",
                Data = null,
                StatusCode = 200
            };
        }

        /// <summary>
        /// Retrieves the conversation history between two users based on specified query parameters.
        /// </summary>
        /// <param name="queryParameters">The query parameters specifying user IDs, timestamp, count, and sort order.</param>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <returns>A list of messages representing the conversation history.</returns>
        public async Task<List<Message?>> GetConversationHistoryAsync(ConversationHistoryDto queryParameters, string currentUserId)
        {
            // Create a query to retrieve messages between two users within the specified timestamp range
            var query = _context.Messages.Where(m =>
                                            (m.SenderId == currentUserId && m.ReceiverId == queryParameters.UserId.ToString()) ||
                                            (m.SenderId == queryParameters.UserId.ToString() && m.ReceiverId == currentUserId))
                                         .Where(m => m.Timestamp <= queryParameters.Before);

            // Sort the messages based on the specified sort order
            if (queryParameters.SortOrder == MinimalChat.Domain.Enum.SortOrder.asc)
            {
                query = query.OrderBy(m => m.Timestamp);
            }
            else
            {
                query = query.OrderByDescending(m => m.Timestamp);
            }

            // Limit the number of messages retrieved based on the specified count
            if (queryParameters.Count > 0)
            {
                query = query.Take(queryParameters.Count);
            }

            // Execute the query and return the conversation history
            return await query.ToListAsync();
        }
    }
}
