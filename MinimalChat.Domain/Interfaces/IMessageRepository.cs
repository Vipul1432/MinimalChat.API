using MinimalChat.Domain.Models;
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
    }
}
