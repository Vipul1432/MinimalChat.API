using Microsoft.EntityFrameworkCore;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinmalChat.Data.Services
{
    public class LogService : ILogService
    {
        private readonly MinimalChatDbContext _context;

        public LogService(MinimalChatDbContext context)
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

        /// <summary>
        /// Retrieves a collection of log entries within a specified time range.
        /// </summary>
        /// <param name="startTime">The start time for log entries retrieval.</param>
        /// <param name="endTime">The end time for log entries retrieval.</param>
        /// <returns>
        /// A collection of log entries that fall within the specified time range.
        /// </returns>
        public async Task<IEnumerable<RequestLog>> GetLogsAsync(DateTime startTime, DateTime endTime)
        {
            return await _context.RequestLogs.Where(log => log.RequestTimestamp >= startTime && log.RequestTimestamp <= endTime).ToListAsync();
        }
    }
}
