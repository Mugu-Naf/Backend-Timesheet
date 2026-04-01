using AutoMapper;
using FirstAPI.Contexts;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Mappings;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using FirstAPI.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FirstAPI.Tests
{
    public class TimesheetServiceTests : IDisposable
    {
        private readonly TimeSheetContext _context;
        private readonly Mock<IRepository<int, Timesheet>> _timesheetRepoMock;
        private readonly Mock<IRepository<int, OvertimeRule>> _overtimeRepoMock;
        private readonly IMapper _mapper;
        private readonly TimesheetService _service;

        public TimesheetServiceTests()
        {
            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new TimeSheetContext(options);

            _timesheetRepoMock = new Mock<IRepository<int, Timesheet>>();
            _overtimeRepoMock  = new Mock<IRepository<int, OvertimeRule>>();

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _service = new TimesheetService(
                _timesheetRepoMock.Object,
                _overtimeRepoMock.Object,
                _mapper);
        }

        public void Dispose() => _context.Dispose();

        // ── CreateTimesheet Tests ────────────────────────────────────

        [Fact]
        public async Task CreateTimesheet_Weekday_CalculatesOvertimeCorrectly()
        {
            // Arrange — Monday, 10 hours worked, rule says 8 max
            var monday = GetNextWeekday(DayOfWeek.Monday);

            _timesheetRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Timesheets);

            _context.OvertimeRules.Add(new OvertimeRule
            {
                OvertimeRuleId    = 1,
                RuleName          = "Standard",
                MaxRegularHours   = 8.0m,
                OvertimeMultiplier = 1.5m,
                EffectiveFrom     = DateTime.Today.AddYears(-1),
                IsActive          = true
            });
            await _context.SaveChangesAsync();

            _overtimeRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.OvertimeRules);

            _timesheetRepoMock.Setup(r => r.Add(It.IsAny<Timesheet>()))
                .ReturnsAsync((Timesheet t) => { t.TimesheetId = 1; return t; });

            var dto = new TimesheetCreateDto
            {
                Date        = monday,
                HoursWorked = 10m
            };

            // Act
            var result = await _service.CreateTimesheet(1, dto);

            // Assert — 10 - 8 = 2 overtime hours
            Assert.Equal(2m, result.OvertimeHours);
            Assert.False(result.IsWeekend);
        }

        [Fact]
        public async Task CreateTimesheet_Weekend_AllHoursAreOvertime()
        {
            // Arrange — Saturday, 8 hours worked
            var saturday = GetNextWeekday(DayOfWeek.Saturday);

            _timesheetRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Timesheets);

            _overtimeRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.OvertimeRules);

            _timesheetRepoMock.Setup(r => r.Add(It.IsAny<Timesheet>()))
                .ReturnsAsync((Timesheet t) => { t.TimesheetId = 1; return t; });

            var dto = new TimesheetCreateDto
            {
                Date        = saturday,
                HoursWorked = 8m
            };

            // Act
            var result = await _service.CreateTimesheet(1, dto);

            // Assert — weekend: all 8 hours = overtime, multiplier = 2x
            Assert.Equal(8m, result.OvertimeHours);
            Assert.True(result.IsWeekend);
            Assert.Equal(2.0m, result.OvertimeMultiplier);
        }

        [Fact]
        public async Task CreateTimesheet_DuplicateDate_ThrowsDuplicateEntityException()
        {
            // Arrange — existing timesheet for same date
            var date = DateTime.Today.AddDays(-1);

            _context.Timesheets.Add(new Timesheet
            {
                TimesheetId = 1,
                EmployeeId  = 1,
                Date        = date,
                HoursWorked = 8m,
                Status      = TimesheetStatus.Pending
            });
            await _context.SaveChangesAsync();

            _timesheetRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Timesheets);

            var dto = new TimesheetCreateDto { Date = date, HoursWorked = 8m };

            // Act & Assert
            await Assert.ThrowsAsync<DuplicateEntityException>(
                () => _service.CreateTimesheet(1, dto));
        }

        [Fact]
        public async Task CreateTimesheet_NoOvertimeRule_DefaultsTo8Hours()
        {
            // Arrange — no overtime rule, 9 hours worked
            var monday = GetNextWeekday(DayOfWeek.Monday);

            _timesheetRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Timesheets);

            _overtimeRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.OvertimeRules); // empty

            _timesheetRepoMock.Setup(r => r.Add(It.IsAny<Timesheet>()))
                .ReturnsAsync((Timesheet t) => { t.TimesheetId = 1; return t; });

            var dto = new TimesheetCreateDto { Date = monday, HoursWorked = 9m };

            // Act
            var result = await _service.CreateTimesheet(1, dto);

            // Assert — default 8h max, so 1h overtime
            Assert.Equal(1m, result.OvertimeHours);
        }

        // ── ApproveTimesheet Tests ───────────────────────────────────

        [Fact]
        public async Task ApproveTimesheet_PendingStatus_ChangesToApproved()
        {
            // Arrange
            var timesheet = new Timesheet
            {
                TimesheetId = 1,
                EmployeeId  = 1,
                Date        = DateTime.Today.AddDays(-1),
                HoursWorked = 8m,
                Status      = TimesheetStatus.Pending
            };
            _context.Timesheets.Add(timesheet);
            await _context.SaveChangesAsync();

            _timesheetRepoMock.Setup(r => r.Get(1)).ReturnsAsync(timesheet);
            _timesheetRepoMock.Setup(r => r.GetQueryable()).Returns(_context.Timesheets);
            _timesheetRepoMock.Setup(r => r.Update(It.IsAny<Timesheet>()))
                .ReturnsAsync((Timesheet t) => t);

            // Act
            var result = await _service.ApproveTimesheet(1, "admin");

            // Assert
            Assert.Equal("Approved", result.Status);
        }

        [Fact]
        public async Task ApproveTimesheet_AlreadyApproved_ThrowsValidationException()
        {
            // Arrange
            var timesheet = new Timesheet
            {
                TimesheetId = 1,
                EmployeeId  = 1,
                Date        = DateTime.Today.AddDays(-1),
                HoursWorked = 8m,
                Status      = TimesheetStatus.Approved // already approved
            };

            _timesheetRepoMock.Setup(r => r.Get(1)).ReturnsAsync(timesheet);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.ApproveTimesheet(1, "admin"));
        }

        // ── RejectTimesheet Tests ────────────────────────────────────

        [Fact]
        public async Task RejectTimesheet_PendingStatus_ChangesToRejected()
        {
            // Arrange
            var timesheet = new Timesheet
            {
                TimesheetId = 1,
                EmployeeId  = 1,
                Date        = DateTime.Today.AddDays(-1),
                HoursWorked = 8m,
                Status      = TimesheetStatus.Pending
            };
            _context.Timesheets.Add(timesheet);
            await _context.SaveChangesAsync();

            _timesheetRepoMock.Setup(r => r.Get(1)).ReturnsAsync(timesheet);
            _timesheetRepoMock.Setup(r => r.GetQueryable()).Returns(_context.Timesheets);
            _timesheetRepoMock.Setup(r => r.Update(It.IsAny<Timesheet>()))
                .ReturnsAsync((Timesheet t) => t);

            // Act
            var result = await _service.RejectTimesheet(1, "admin");

            // Assert
            Assert.Equal("Rejected", result.Status);
        }

        // ── UpdateTimesheet Tests ────────────────────────────────────

        [Fact]
        public async Task UpdateTimesheet_NotOwner_ThrowsUnAuthorizedException()
        {
            // Arrange
            var timesheet = new Timesheet
            {
                TimesheetId = 1,
                EmployeeId  = 1, // owned by employee 1
                Date        = DateTime.Today.AddDays(-1),
                HoursWorked = 8m,
                Status      = TimesheetStatus.Pending
            };

            _timesheetRepoMock.Setup(r => r.Get(1)).ReturnsAsync(timesheet);

            var dto = new TimesheetUpdateDto { HoursWorked = 9m };

            // Act & Assert — employee 2 trying to update employee 1's timesheet
            await Assert.ThrowsAsync<UnAuthorizedException>(
                () => _service.UpdateTimesheet(1, 2, dto));
        }

        [Fact]
        public async Task UpdateTimesheet_NotPending_ThrowsValidationException()
        {
            // Arrange
            var timesheet = new Timesheet
            {
                TimesheetId = 1,
                EmployeeId  = 1,
                Date        = DateTime.Today.AddDays(-1),
                HoursWorked = 8m,
                Status      = TimesheetStatus.Approved // can't update approved
            };

            _timesheetRepoMock.Setup(r => r.Get(1)).ReturnsAsync(timesheet);

            var dto = new TimesheetUpdateDto { HoursWorked = 9m };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.UpdateTimesheet(1, 1, dto));
        }

        // ── DeleteTimesheet Tests ────────────────────────────────────

        [Fact]
        public async Task DeleteTimesheet_NotOwner_ThrowsUnAuthorizedException()
        {
            var timesheet = new Timesheet
            {
                TimesheetId = 1,
                EmployeeId  = 1,
                Status      = TimesheetStatus.Pending
            };

            _timesheetRepoMock.Setup(r => r.Get(1)).ReturnsAsync(timesheet);

            await Assert.ThrowsAsync<UnAuthorizedException>(
                () => _service.DeleteTimesheet(1, 2));
        }

        // ── Helper ──────────────────────────────────────────────────

        private static DateTime GetNextWeekday(DayOfWeek day)
        {
            var date = DateTime.Today.AddDays(-7); // go back a week to get a past date
            while (date.DayOfWeek != day)
                date = date.AddDays(1);
            return date;
        }
    }
}
