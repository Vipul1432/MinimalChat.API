using Microsoft.EntityFrameworkCore;
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

    }
}
