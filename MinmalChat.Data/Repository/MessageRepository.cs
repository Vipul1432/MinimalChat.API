using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
