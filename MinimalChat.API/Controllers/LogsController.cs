using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using MinmalChat.Data.Services;
using System.Net;

namespace MinimalChat.API.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize] 
    public class LogsController : ControllerBase
    {
        private readonly ILogService _logService;

        public LogsController(ILogService logService)
        {
            _logService = logService;
        }

        #region Retrive logs in specific time range

        /// <summary>
        /// Retrieves logs within a specified time range.
        /// </summary>
        /// <param name="startTime">The start time of the log entries (optional, default: Current Timestamp - 5 minutes).</param>
        /// <param name="endTime">The end time of the log entries (optional, default: Current Timestamp).</param>
        /// <returns>
        /// - 200 OK: Log list received successfully.
        /// - 400 Bad Request: Invalid request parameters or no logs found.
        /// - 500 Internal Server Error: An unexpected error occurred.
        /// </returns>
        [HttpGet("log")]
        public async Task<IActionResult> GetLogsAsync([FromQuery] DateTime? startTime = null, [FromQuery] DateTime? endTime = null)
        {
            try
           {
                // Validate and set default values for start and end time if not provided
                if (!startTime.HasValue)
                {
                    startTime = DateTime.Now.AddMinutes(-5); // Default: Current Timestamp - 5 minutes
                }

                if (!endTime.HasValue)
                {
                    endTime = DateTime.Now; // Default: Current Timestamp
                }

                // Retrieve logs based on the provided time range
                IEnumerable<RequestLog> logs = await _logService.GetLogsAsync(startTime.Value, endTime.Value);

                if (logs.Any())
                {
                    // Return the logs in the response body
                    return Ok(new ApiResponse<IEnumerable<RequestLog>>
                    {
                        Message = "Log list received successfully!",
                        Data = logs,
                        StatusCode = HttpStatusCode.OK
                    });
                }
                else
                {
                    // No logs found
                    return NotFound(new ApiResponse<RequestLog>
                    {
                        Message = "No logs found.",
                        Data = null, 
                        StatusCode = HttpStatusCode.NotFound
                    });                
                }
            }
            catch (Exception)
            {
                return new ObjectResult(new ApiResponse<Object>
                {
                    Message = "An error occurred! Please try again.",
                    Data = null,
                    StatusCode = HttpStatusCode.InternalServerError
                });
            }
        }

        #endregion Retrive logs in specific time range
    }
}
