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
    public class RequestLogRepository : IRequestLogRepository
    {
        private readonly MinimalChatDbContext _context;

        public RequestLogRepository(MinimalChatDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Asynchronously adds a <see cref="RequestLog"/> object to the database context and saves changes.
        /// </summary>
        /// <param name="requestLog">The <see cref="RequestLog"/> to be added to the database.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddAsync(RequestLog requestLog)
        {
            _context.RequestLogs.Add(requestLog);
            await _context.SaveChangesAsync();
        }
    }
}
