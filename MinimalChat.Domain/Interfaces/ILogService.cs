using MinimalChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Interfaces
{
    public interface ILogService
    {
        /// <summary>
        /// Asynchronously adds a <see cref="RequestLog"/> object to the database context and saves changes.
        /// </summary>
        /// <param name="requestLog">The <see cref="RequestLog"/> to be added to the database.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(RequestLog requestLog);

        /// <summary>
        /// Retrieves a collection of log entries within a specified time range.
        /// </summary>
        /// <param name="startTime">The start time for log entries retrieval.</param>
        /// <param name="endTime">The end time for log entries retrieval.</param>
        /// <returns>
        /// A collection of log entries that fall within the specified time range.
        /// </returns>
        Task<IEnumerable<RequestLog>> GetLogsAsync(DateTime startTime, DateTime endTime);
    }
}
