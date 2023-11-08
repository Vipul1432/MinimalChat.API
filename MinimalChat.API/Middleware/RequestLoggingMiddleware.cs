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

        /// <summary>
        /// Middleware component for logging incoming HTTP requests and responses, including details
        /// such as the request IP address, timestamp, user identity (if authenticated), and request body.
        /// The logged information is stored in a database using the provided log service.
        /// In case of exceptions, it returns a 500 Internal Server Error response with an error message.
        /// </summary>
        /// <param name="context">The HTTP context of the current request.</param>
        /// <param name="_logService">A service for logging request details to a database.</param>
        /// <returns>An asynchronous task representing the middleware's processing of the request.</returns>
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
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
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
        /// <summary>
        /// Extension method to register the custom Request Logging Middleware in the application's request processing pipeline.
        /// This middleware logs incoming HTTP requests and responses, capturing details such as IP address, timestamp,
        /// user identity (if authenticated), and request body. Logged data is stored in a database.
        /// </summary>
        /// <param name="builder">The application builder to which the middleware should be added.</param>
        /// <returns>The modified application builder with the middleware added to the pipeline.</returns>
        public static IApplicationBuilder UseRequestLoggingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
