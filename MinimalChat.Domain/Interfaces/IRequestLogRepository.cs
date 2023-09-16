using MinimalChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Interfaces
{
    public interface IRequestLogRepository
    {
        /// <summary>
        /// Asynchronously adds a <see cref="RequestLog"/> object to the database context and saves changes.
        /// </summary>
        /// <param name="requestLog">The <see cref="RequestLog"/> to be added to the database.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(RequestLog requestLog);
    }
}
