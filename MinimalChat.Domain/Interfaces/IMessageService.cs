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
    public interface IMessageService
    {
        /// <summary>
        /// Asynchronously sends a chat message.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <returns>A task representing the result of the send operation.</returns>
        Task<bool> SendMessageAsync(Message message);

        /// <summary>
        /// Asynchronously edits a chat message with the specified ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to edit.</param>
        /// <param name="updatedContent">The updated content for the message.</param>
        /// <param name="currentUserId">The ID of the current user editing the message.</param>
        /// <returns>An ApiResponse containing the result of the edit operation.</returns>
        Task<ApiResponse<Message>> EditMessageAsync(int messageId, string updatedContent, string currentUserId);

        /// <summary>
        /// Asynchronously deletes a chat message with the specified ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to delete.</param>
        /// <param name="currentUserId">The ID of the current user deleting the message.</param>
        /// <returns>An ApiResponse containing the result of the delete operation.</returns>
        Task<ApiResponse<Message>> DeleteMessageAsync(int messageId, string currentUserId);

        /// <summary>
        /// Retrieves a conversation history between two users based on specified query parameters.
        /// </summary>
        /// <param name="queryParameters">The query parameters specifying user IDs, timestamp, count, and sort order.</param>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <returns>A list of messages representing the conversation history.</returns>
        Task<List<Message?>> GetConversationHistoryAsync(ConversationHistoryDto queryParameters, string currentUserId);
    }
}
