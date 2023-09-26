using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILogService _logService)
        {
            try
            {
                var request = context.Request;

                // Enable rewinding the request body so it can be read multiple times
                context.Request.EnableBuffering();

                // Read the request body
                string requestBody;
                using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                // Reset the request body stream position to the beginning
                context.Request.Body.Position = 0;


                // Log the request details
                var requestLog = new RequestLog
                {
                    IpAddress = context.Connection.RemoteIpAddress?.ToString()!,
                    RequestTimestamp = DateTime.Now,
                    Username = context.User.Identity!.IsAuthenticated ? context.User.Identity.Name! : string.Empty, // Fetch username from auth token
                    RequestBody = requestBody,
                };

                // log this information in database using log Repository
                await _logService.AddAsync(requestLog);

                // Continue processing the request
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync($"Internal Server Error: {ex.Message}");
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLoggingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
