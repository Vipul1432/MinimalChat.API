using AutoMapper;
using FluentAssertions;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace MinimalChat.Tests
{
    public class LogServiceTests
    {
        private readonly Mock<IRepository<RequestLog>> _mockLogRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly LogService _logService;

        public LogServiceTests()
        {
            _mockLogRepository = new Mock<IRepository<RequestLog>>();
            _mockMapper = new Mock<IMapper>();
            _logService = new LogService(_mockLogRepository.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task LogRequestAsync_Success()
        {
            // Arrange
            var requestLog = new RequestLog
            {
                Id = Guid.NewGuid(),
                Path = "/api/test",
                Method = "GET",
                Timestamp = DateTime.UtcNow,
                RequestBody = "{}",
                StatusCode = 200
            };

            _mockLogRepository.Setup(repo => repo.AddAsync(It.IsAny<RequestLog>())).Returns(Task.CompletedTask);

            // Act
            await _logService.LogRequestAsync(requestLog);

            // Assert
            _mockLogRepository.Verify(repo => repo.AddAsync(requestLog), Times.Once);
        }

        [Fact]
        public async Task GetLogsAsync_NoDates_ReturnsAllLogs()
        {
            // Arrange
            var logsFromRepo = new List<RequestLog>
            {
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/test1", Timestamp = DateTime.UtcNow.AddHours(-1) },
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/test2", Timestamp = DateTime.UtcNow }
            };
            var expectedLogDtos = logsFromRepo.Select(log => new LogDto { Id = log.Id, Path = log.Path, Timestamp = log.Timestamp }).ToList();

            _mockLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<RequestLog, bool>>>()))
                             .ReturnsAsync(logsFromRepo); // Predicate will be null, so all are returned
            _mockMapper.Setup(m => m.Map<IEnumerable<LogDto>>(logsFromRepo)).Returns(expectedLogDtos);

            // Act
            var result = await _logService.GetLogsAsync(null, null);

            // Assert
            _mockLogRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<RequestLog, bool>>>()), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<LogDto>>(logsFromRepo), Times.Once);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedLogDtos);
        }

        [Fact]
        public async Task GetLogsAsync_WithStartDate_ReturnsLogsFromStartDate()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddHours(-1);
            var allLogs = new List<RequestLog>
            {
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/before", Timestamp = startDate.AddHours(-1) },
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/on_start", Timestamp = startDate },
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/after", Timestamp = startDate.AddHours(1) }
            };
            var filteredLogsFromRepo = allLogs.Where(log => log.Timestamp >= startDate).ToList();
            var expectedLogDtos = filteredLogsFromRepo.Select(log => new LogDto { Id = log.Id, Path = log.Path, Timestamp = log.Timestamp }).ToList();

            _mockLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<RequestLog, bool>>>()))
                             .ReturnsAsync((Expression<Func<RequestLog, bool>> predicate) => allLogs.Where(predicate.Compile()).ToList());
            _mockMapper.Setup(m => m.Map<IEnumerable<LogDto>>(It.Is<IEnumerable<RequestLog>>(logs => logs.SequenceEqual(filteredLogsFromRepo))))
                       .Returns(expectedLogDtos);

            // Act
            var result = await _logService.GetLogsAsync(startDate, null);

            // Assert
            _mockLogRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<RequestLog, bool>>>()), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<LogDto>>(filteredLogsFromRepo), Times.Once);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedLogDtos);
            result.All(log => log.Timestamp >= startDate).Should().BeTrue();
        }

        [Fact]
        public async Task GetLogsAsync_WithEndDate_ReturnsLogsUntilEndDate()
        {
            // Arrange
            var endDate = DateTime.UtcNow.AddHours(1);
            var allLogs = new List<RequestLog>
            {
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/before", Timestamp = endDate.AddHours(-1) },
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/on_end", Timestamp = endDate },
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/after", Timestamp = endDate.AddHours(1) }
            };
            var filteredLogsFromRepo = allLogs.Where(log => log.Timestamp <= endDate).ToList();
            var expectedLogDtos = filteredLogsFromRepo.Select(log => new LogDto { Id = log.Id, Path = log.Path, Timestamp = log.Timestamp }).ToList();

            _mockLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<RequestLog, bool>>>()))
                             .ReturnsAsync((Expression<Func<RequestLog, bool>> predicate) => allLogs.Where(predicate.Compile()).ToList());
            _mockMapper.Setup(m => m.Map<IEnumerable<LogDto>>(It.Is<IEnumerable<RequestLog>>(logs => logs.SequenceEqual(filteredLogsFromRepo))))
                       .Returns(expectedLogDtos);

            // Act
            var result = await _logService.GetLogsAsync(null, endDate);

            // Assert
            _mockLogRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<RequestLog, bool>>>()), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<LogDto>>(filteredLogsFromRepo), Times.Once);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedLogDtos);
            result.All(log => log.Timestamp <= endDate).Should().BeTrue();
        }

        [Fact]
        public async Task GetLogsAsync_WithBothDates_ReturnsLogsInDateRange()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-1);
            var endDate = DateTime.UtcNow;
            var allLogs = new List<RequestLog>
            {
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/too_early", Timestamp = startDate.AddDays(-1) },
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/on_start", Timestamp = startDate },
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/in_range", Timestamp = startDate.AddHours(12) },
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/on_end", Timestamp = endDate },
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/too_late", Timestamp = endDate.AddDays(1) }
            };
            var filteredLogsFromRepo = allLogs.Where(log => log.Timestamp >= startDate && log.Timestamp <= endDate).ToList();
            var expectedLogDtos = filteredLogsFromRepo.Select(log => new LogDto { Id = log.Id, Path = log.Path, Timestamp = log.Timestamp }).ToList();

            _mockLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<RequestLog, bool>>>()))
                             .ReturnsAsync((Expression<Func<RequestLog, bool>> predicate) => allLogs.Where(predicate.Compile()).ToList());
            _mockMapper.Setup(m => m.Map<IEnumerable<LogDto>>(It.Is<IEnumerable<RequestLog>>(logs => logs.SequenceEqual(filteredLogsFromRepo))))
                       .Returns(expectedLogDtos);

            // Act
            var result = await _logService.GetLogsAsync(startDate, endDate);

            // Assert
            _mockLogRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<RequestLog, bool>>>()), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<LogDto>>(filteredLogsFromRepo), Times.Once);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedLogDtos);
            result.All(log => log.Timestamp >= startDate && log.Timestamp <= endDate).Should().BeTrue();
        }

        [Fact]
        public async Task GetLogsAsync_NoLogsMatch_ReturnsEmptyList()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(1); // Future start date to ensure no logs match
            var endDate = DateTime.UtcNow.AddDays(2);
            var allLogs = new List<RequestLog> // Logs from the past
            {
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/past_log1", Timestamp = DateTime.UtcNow.AddDays(-1) },
                new RequestLog { Id = Guid.NewGuid(), Path = "/api/past_log2", Timestamp = DateTime.UtcNow.AddDays(-2) }
            };
            // The predicate will filter allLogs to an empty list
            var filteredLogsFromRepo = new List<RequestLog>();
            var expectedLogDtos = new List<LogDto>(); // Empty list

            _mockLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<RequestLog, bool>>>()))
                             .ReturnsAsync((Expression<Func<RequestLog, bool>> predicate) => allLogs.Where(predicate.Compile()).ToList());
            _mockMapper.Setup(m => m.Map<IEnumerable<LogDto>>(It.Is<IEnumerable<RequestLog>>(logs => !logs.Any())))
                       .Returns(expectedLogDtos);

            // Act
            var result = await _logService.GetLogsAsync(startDate, endDate);

            // Assert
            _mockLogRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<RequestLog, bool>>>()), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<LogDto>>(filteredLogsFromRepo), Times.Once);
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}
