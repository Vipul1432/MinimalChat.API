using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MinimalChat.API.Controllers;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MinimalChat.Tests
{
    public class LogsControllerTests
    {
        private readonly Mock<ILogService> _mockLogService;
        private readonly Mock<ILogger<LogsController>> _mockLogger;
        private readonly LogsController _controller;

        public LogsControllerTests()
        {
            _mockLogService = new Mock<ILogService>();
            _mockLogger = new Mock<ILogger<LogsController>>();
            _controller = new LogsController(_mockLogService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetLogs_NoDates_ReturnsOkWithAllLogs()
        {
            // Arrange
            var expectedLogs = new List<LogDto>
            {
                new LogDto { Path = "/api/test1", Timestamp = DateTime.UtcNow.AddHours(-1) },
                new LogDto { Path = "/api/test2", Timestamp = DateTime.UtcNow }
            };
            _mockLogService.Setup(s => s.GetLogsAsync(null, null)).ReturnsAsync(expectedLogs);

            // Act
            var result = await _controller.GetLogs(null, null);

            // Assert
            _mockLogService.Verify(s => s.GetLogsAsync(null, null), Times.Once);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedLogs);
        }

        [Fact]
        public async Task GetLogs_WithStartDate_ReturnsOkWithFilteredLogs()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date;
            var expectedLogs = new List<LogDto>
            {
                new LogDto { Path = "/api/test_start", Timestamp = startDate.AddHours(1) }
            };
            _mockLogService.Setup(s => s.GetLogsAsync(startDate, null)).ReturnsAsync(expectedLogs);

            // Act
            var result = await _controller.GetLogs(startDate, null);

            // Assert
            _mockLogService.Verify(s => s.GetLogsAsync(startDate, null), Times.Once);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedLogs);
        }

        [Fact]
        public async Task GetLogs_WithEndDate_ReturnsOkWithFilteredLogs()
        {
            // Arrange
            var endDate = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1); // End of the day
            var expectedLogs = new List<LogDto>
            {
                new LogDto { Path = "/api/test_end", Timestamp = endDate.AddHours(-1) }
            };
            _mockLogService.Setup(s => s.GetLogsAsync(null, endDate)).ReturnsAsync(expectedLogs);

            // Act
            var result = await _controller.GetLogs(null, endDate);

            // Assert
            _mockLogService.Verify(s => s.GetLogsAsync(null, endDate), Times.Once);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedLogs);
        }

        [Fact]
        public async Task GetLogs_WithBothDates_ReturnsOkWithFilteredLogs()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date;
            var endDate = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
            var expectedLogs = new List<LogDto>
            {
                new LogDto { Path = "/api/test_both", Timestamp = startDate.AddHours(5) }
            };
            _mockLogService.Setup(s => s.GetLogsAsync(startDate, endDate)).ReturnsAsync(expectedLogs);

            // Act
            var result = await _controller.GetLogs(startDate, endDate);

            // Assert
            _mockLogService.Verify(s => s.GetLogsAsync(startDate, endDate), Times.Once);
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedLogs);
        }

        [Fact]
        public async Task GetLogs_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var exceptionMessage = "Service layer error";
            _mockLogService.Setup(s => s.GetLogsAsync(null, null)).ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.GetLogs(null, null);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().Be($"An error occurred while fetching logs: {exceptionMessage}");
        }

        [Fact]
        public async Task GetLogs_InvalidDateRange_StartDateAfterEndDate_ReturnsBadRequest()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date.AddDays(1);
            var endDate = DateTime.UtcNow.Date; // Start date is after end date

            // Act
            var result = await _controller.GetLogs(startDate, endDate);

            // Assert
            _mockLogService.Verify(s => s.GetLogsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Never);
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("Start date cannot be after end date.");
        }
    }
}
