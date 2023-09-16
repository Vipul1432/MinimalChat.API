using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Interfaces
{
    public interface IMessageRepository
    {
        /// <summary>
        /// Adds a message to the database and saves changes asynchronously.
        /// </summary>
        /// <param name="message">The message to be added and saved.</param>
        /// <returns>A boolean indicating whether the message was successfully sent and saved.</returns>
        Task<bool> SendMessageAsync(Message message);

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
        Task<ApiResponse<Message>> EditMessageAsync(int messageId, string updatedContent, string currentUserId);

        /// <summary>
        /// Deletes a message with the specified message ID if it exists and if the current user is the sender.
        /// </summary>
        /// <param name="messageId">The ID of the message to delete.</param>
        /// <param name="currentUserId">The ID of the current authenticated user.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> indicating the result of the delete operation.</returns>
        Task<ApiResponse<Message>> DeleteMessageAsync(int messageId, string currentUserId);

        /// <summary>
        /// Retrieves the conversation history between two users based on specified query parameters.
        /// </summary>
        /// <param name="queryParameters">The query parameters specifying user IDs, timestamp, count, and sort order.</param>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <returns>A list of messages representing the conversation history.</returns>
        Task<List<Message?>> GetConversationHistoryAsync(ConversationHistoryDto queryParameters,string currentUserId);
    }
}
