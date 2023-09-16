using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;

namespace MinimalChat.API.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize] 
    public class LogsController : ControllerBase
    {
        private readonly IRequestLogRepository _requestLogRepository;

        public LogsController(IRequestLogRepository requestLogRepository)
        {
            _requestLogRepository = requestLogRepository;
        }
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
        public async Task<IActionResult> GetLogs([FromQuery] DateTime? startTime = null, [FromQuery] DateTime? endTime = null)
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
                IEnumerable<RequestLog> logs = await _requestLogRepository.GetLogsAsync(startTime.Value, endTime.Value);

                if (logs.Any())
                {
                    // Return the logs in the response body
                    return Ok(new ApiResponse<IEnumerable<RequestLog>>
                    {
                        Message = "Log list received successfully!",
                        Data = logs,
                        StatusCode = 200
                    });
                }
                else
                {
                    // No logs found
                    return NotFound(new ApiResponse<RequestLog>
                    {
                        Message = "No logs found.",
                        Data = null, 
                        StatusCode = 400
                    });                
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<RequestLog>
                {
                    Message = ex.Message,
                    Data = null,
                    StatusCode = 500
                });
            }
        }
    }
}
