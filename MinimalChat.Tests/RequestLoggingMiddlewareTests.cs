using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging; // Though not directly used, good for context
using MinimalChat.API.Middleware;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using Moq;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace MinimalChat.Tests
{
    public class RequestLoggingMiddlewareTests
    {
        private readonly Mock<ILogService> _mockLogService;
        private readonly Mock<RequestDelegate> _mockNextDelegate;
        private readonly RequestLoggingMiddleware _middleware;
        private readonly DefaultHttpContext _httpContext;
        private readonly Mock<IServiceProvider> _mockServiceProvider;

        public RequestLoggingMiddlewareTests()
        {
            _mockLogService = new Mock<ILogService>();
            _mockNextDelegate = new Mock<RequestDelegate>();
            _middleware = new RequestLoggingMiddleware(_mockNextDelegate.Object);
            _httpContext = new DefaultHttpContext();

            // Mock IServiceProvider to provide ILogService
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(ILogService)))
                .Returns(_mockLogService.Object);
            _httpContext.RequestServices = _mockServiceProvider.Object;

            // Default request setup
            _httpContext.Request.Scheme = "http";
            _httpContext.Request.Host = new HostString("localhost");
            _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        }

        [Fact]
        public async Task InvokeAsync_LogsRequestAndCallsNext()
        {
            // Arrange
            _httpContext.Request.Path = "/test/path";
            _httpContext.Request.QueryString = new QueryString("?param=value");
            _httpContext.Request.Method = "GET";
            // For RequestBody, if middleware reads it (it does)
            var requestBodyContent = "{\"key\":\"value\"}";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBodyContent));
            _httpContext.Request.Body = stream;
            _httpContext.Request.ContentType = "application/json";


            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _mockLogService.Verify(service => service.LogRequestAsync(It.Is<RequestLog>(log =>
                log.Path == "/test/path" &&
                log.QueryString == "?param=value" &&
                log.Method == "GET" &&
                log.Host == "localhost" &&
                log.ClientIp == "127.0.0.1" &&
                log.RequestBody.Contains("key") && // Check if body was read
                log.RequestBody.Contains("value") &&
                log.Timestamp <= DateTime.UtcNow && log.Timestamp > DateTime.UtcNow.AddSeconds(-5) // Timestamp is recent
            )), Times.Once);

            _mockNextDelegate.Verify(next => next(_httpContext), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_LogServiceThrowsException_StillCallsNext()
        {
            // Arrange
            _httpContext.Request.Path = "/test/error";
            _httpContext.Request.Method = "POST";
            var requestBodyContent = "{\"data\":\"important\"}";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBodyContent));
            _httpContext.Request.Body = stream;
            _httpContext.Request.ContentType = "application/json";


            _mockLogService.Setup(s => s.LogRequestAsync(It.IsAny<RequestLog>()))
                           .ThrowsAsync(new Exception("Failed to log to database"));

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _mockLogService.Verify(service => service.LogRequestAsync(It.IsAny<RequestLog>()), Times.Once);
            _mockNextDelegate.Verify(next => next(_httpContext), Times.Once); // Crucial: next delegate must still be called
        }

        [Fact]
        public async Task InvokeAsync_DoesNotLogHealthCheckEndpoint()
        {
            // Arrange
            _httpContext.Request.Path = "/health";
            _httpContext.Request.Method = "GET";
             var requestBodyContent = "{}"; // Health checks usually have no body or simple one
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBodyContent));
            _httpContext.Request.Body = stream;
            _httpContext.Request.ContentType = "application/json";


            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _mockLogService.Verify(service => service.LogRequestAsync(It.IsAny<RequestLog>()), Times.Never());
            _mockNextDelegate.Verify(next => next(_httpContext), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_HandlesNullRemoteIpAddressGracefully()
        {
            // Arrange
            _httpContext.Request.Path = "/test/no-ip";
            _httpContext.Request.Method = "GET";
            _httpContext.Connection.RemoteIpAddress = null; // Simulate missing IP

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _mockLogService.Verify(service => service.LogRequestAsync(It.Is<RequestLog>(log =>
                log.ClientIp == "Unknown" && // Or whatever default/fallback is used
                log.Path == "/test/no-ip"
            )), Times.Once);
            _mockNextDelegate.Verify(next => next(_httpContext), Times.Once);
        }
        
        [Fact]
        public async Task InvokeAsync_LogsRequestWithEmptyQueryString()
        {
            // Arrange
            _httpContext.Request.Path = "/test/noquery";
            _httpContext.Request.Method = "GET";
            _httpContext.Request.QueryString = QueryString.Empty; // No query string

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _mockLogService.Verify(service => service.LogRequestAsync(It.Is<RequestLog>(log =>
                log.Path == "/test/noquery" &&
                log.QueryString == "" && // Ensure empty string is logged
                log.Method == "GET"
            )), Times.Once);
            _mockNextDelegate.Verify(next => next(_httpContext), Times.Once);
        }
    }
}
