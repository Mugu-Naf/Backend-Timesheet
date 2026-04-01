using FirstAPI.Contexts;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using FirstAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FirstAPI.Tests
{
    public class AuditLogServiceTests : IDisposable
    {
        private readonly TimeSheetContext _context;
        private readonly Mock<ILogger<AuditLogService>> _loggerMock;
        private readonly AuditLogService _service;

        public AuditLogServiceTests()
        {
            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context    = new TimeSheetContext(options);
            _loggerMock = new Mock<ILogger<AuditLogService>>();
            _service    = new AuditLogService(_context, _loggerMock.Object);
        }

        public void Dispose() => _context.Dispose();

        // ── LogAsync Tests ───────────────────────────────────────────

        [Fact]
        public async Task LogAsync_ValidData_SavesLogToDatabase()
        {
            // Act
            await _service.LogAsync("john", "LOGIN", "User", null, "User logged in", "127.0.0.1");

            // Assert
            var log = await _context.AuditLogs.FirstOrDefaultAsync();
            Assert.NotNull(log);
            Assert.Equal("john", log.Username);
            Assert.Equal("LOGIN", log.Action);
            Assert.Equal("User", log.EntityType);
            Assert.Equal("Success", log.Status);
        }

        [Fact]
        public async Task LogAsync_WithEntityId_SavesEntityId()
        {
            // Act
            await _service.LogAsync("admin", "DELETE", "Employee", 42, "Deleted employee");

            // Assert
            var log = await _context.AuditLogs.FirstOrDefaultAsync();
            Assert.NotNull(log);
            Assert.Equal(42, log.EntityId);
            Assert.Equal("DELETE", log.Action);
        }

        [Fact]
        public async Task LogAsync_WithCustomStatus_SavesStatus()
        {
            // Act
            await _service.LogAsync("john", "LOGIN", "User", null, "Failed login", status: "Failed");

            // Assert
            var log = await _context.AuditLogs.FirstOrDefaultAsync();
            Assert.NotNull(log);
            Assert.Equal("Failed", log.Status);
        }

        [Fact]
        public async Task LogAsync_ExceptionThrown_DoesNotPropagateException()
        {
            // Arrange — use a disposed context to force an exception
            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var disposedContext = new TimeSheetContext(options);
            disposedContext.Dispose();

            var service = new AuditLogService(disposedContext, _loggerMock.Object);

            // Act — should NOT throw even if DB fails
            var exception = await Record.ExceptionAsync(
                () => service.LogAsync("john", "LOGIN", "User"));

            // Assert — audit logging never breaks the main flow
            Assert.Null(exception);
        }

        // ── GetLogsAsync Tests ───────────────────────────────────────

        [Fact]
        public async Task GetLogsAsync_NoFilter_ReturnsAllLogs()
        {
            // Arrange
            _context.AuditLogs.AddRange(
                new AuditLog { Username = "john",  Action = "LOGIN",  EntityType = "User",     Timestamp = DateTime.UtcNow, Status = "Success" },
                new AuditLog { Username = "admin", Action = "DELETE", EntityType = "Employee", Timestamp = DateTime.UtcNow, Status = "Success" },
                new AuditLog { Username = "john",  Action = "CREATE", EntityType = "Timesheet", Timestamp = DateTime.UtcNow, Status = "Success" }
            );
            await _context.SaveChangesAsync();

            var filter = new AuditLogFilterDto { Page = 1, PageSize = 50 };

            // Act
            var result = await _service.GetLogsAsync(filter);

            // Assert
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count());
        }

        [Fact]
        public async Task GetLogsAsync_FilterByUsername_ReturnsMatchingLogs()
        {
            // Arrange
            _context.AuditLogs.AddRange(
                new AuditLog { Username = "john",  Action = "LOGIN",  EntityType = "User",     Timestamp = DateTime.UtcNow, Status = "Success" },
                new AuditLog { Username = "admin", Action = "DELETE", EntityType = "Employee", Timestamp = DateTime.UtcNow, Status = "Success" }
            );
            await _context.SaveChangesAsync();

            var filter = new AuditLogFilterDto { Username = "john", Page = 1, PageSize = 50 };

            // Act
            var result = await _service.GetLogsAsync(filter);

            // Assert
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Items, l => Assert.Equal("john", l.Username));
        }

        [Fact]
        public async Task GetLogsAsync_FilterByAction_ReturnsMatchingLogs()
        {
            // Arrange
            _context.AuditLogs.AddRange(
                new AuditLog { Username = "john",  Action = "LOGIN",  EntityType = "User",     Timestamp = DateTime.UtcNow, Status = "Success" },
                new AuditLog { Username = "john",  Action = "CREATE", EntityType = "Timesheet", Timestamp = DateTime.UtcNow, Status = "Success" },
                new AuditLog { Username = "admin", Action = "LOGIN",  EntityType = "User",     Timestamp = DateTime.UtcNow, Status = "Success" }
            );
            await _context.SaveChangesAsync();

            var filter = new AuditLogFilterDto { Action = "LOGIN", Page = 1, PageSize = 50 };

            // Act
            var result = await _service.GetLogsAsync(filter);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, l => Assert.Equal("LOGIN", l.Action));
        }

        [Fact]
        public async Task GetLogsAsync_Pagination_ReturnsCorrectPage()
        {
            // Arrange — 5 logs, page size 2
            for (int i = 1; i <= 5; i++)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Username   = $"user{i}",
                    Action     = "LOGIN",
                    EntityType = "User",
                    Timestamp  = DateTime.UtcNow,
                    Status     = "Success"
                });
            }
            await _context.SaveChangesAsync();

            var filter = new AuditLogFilterDto { Page = 1, PageSize = 2 };

            // Act
            var result = await _service.GetLogsAsync(filter);

            // Assert
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.TotalPages); // ceil(5/2) = 3
            Assert.Equal(2, result.Items.Count());
        }

        // ── GetRecentLogsAsync Tests ─────────────────────────────────

        [Fact]
        public async Task GetRecentLogsAsync_ReturnsTopNLogs()
        {
            // Arrange — 5 logs, ask for 3
            for (int i = 1; i <= 5; i++)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Username   = $"user{i}",
                    Action     = "LOGIN",
                    EntityType = "User",
                    Timestamp  = DateTime.UtcNow.AddMinutes(-i),
                    Status     = "Success"
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetRecentLogsAsync(3);

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task GetRecentLogsAsync_ReturnsNewestFirst()
        {
            // Arrange
            _context.AuditLogs.AddRange(
                new AuditLog { Username = "old",  Action = "LOGIN", EntityType = "User", Timestamp = DateTime.UtcNow.AddHours(-2), Status = "Success" },
                new AuditLog { Username = "new",  Action = "LOGIN", EntityType = "User", Timestamp = DateTime.UtcNow.AddMinutes(-1), Status = "Success" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = (await _service.GetRecentLogsAsync(2)).ToList();

            // Assert — newest first
            Assert.Equal("new", result[0].Username);
            Assert.Equal("old", result[1].Username);
        }
    }
}
