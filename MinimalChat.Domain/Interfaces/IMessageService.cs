﻿using MinimalChat.Domain.DTOs;
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
        Task<ApiResponse<ResponseMessageDto>> EditMessageAsync(int messageId, string updatedContent, string currentUserId);

        /// <summary>
        /// Asynchronously deletes a chat message with the specified ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to delete.</param>
        /// <param name="currentUserId">The ID of the current user deleting the message.</param>
        /// <returns>An ApiResponse containing the result of the delete operation.</returns>
        Task<ApiResponse<ResponseMessageDto>> DeleteMessageAsync(int messageId, string currentUserId);

        /// <summary>
        /// Retrieves a conversation history between two users based on specified query parameters.
        /// </summary>
        /// <param name="queryParameters">The query parameters specifying user IDs, timestamp, count, and sort order.</param>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <returns>A list of messages representing the conversation history.</returns>
        Task<GroupMessageDto?> GetConversationHistoryAsync(ConversationHistoryDto queryParameters, string currentUserId);

        /// <summary>
        /// Searches conversations for messages containing a provided keyword.
        /// </summary>
        /// <param name="query">The keyword to search for within conversations.</param>
        /// <param name="receiverId">The ID of the receiver user for filtering conversations.</param>
        /// <param name="currentUserId">The ID of the current user initiating the search.</param>
        /// <returns>
        /// A list of messages matching the keyword within conversations.
        /// </returns>
        Task<List<ResponseMessageDto>> SearchConversationsAsync(string query, string currentUserId);

        /// <summary>
        /// Asynchronously uploads a file based on the provided information in the 'fileUploadDto.'
        /// </summary>
        /// <param name="fileUploadDto">Data transfer object containing file information and upload details.</param>
        /// <returns>A task that represents the asynchronous operation, returning a string representing the uploaded file's path if successful.</returns>
        /// <remarks>
        /// This method is used to asynchronously upload a file and returns the file's path upon successful completion. The operation is based on the information provided in the 'fileUploadDto.'
        /// </remarks>
        Task<string> UploadFileAsync(FileUploadDto fileUploadDto);

        /// <summary>
        /// Asynchronously retrieves a message by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the message to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation, returning a 'Message' object when the message is successfully retrieved.</returns>
        /// <remarks>
        /// This method is used to asynchronously fetch a message by its unique identifier. It returns a 'Message' object when the specified message is found and retrieved.
        /// </remarks>
        Task<ResponseMessageDto> GetMessageByIdAsync(int id);
    }
}
