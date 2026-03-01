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
    [TestFixture]
    public class TimesheetServiceTests
    {
        private Mock<IRepository<int, Timesheet>> _timesheetRepoMock;
        private Mock<IRepository<int, OvertimeRule>> _overtimeRuleRepoMock;
        private TimeSheetContext _context;
        private IMapper _mapper;
        private TimesheetService _service;

        [SetUp]
        public void Setup()
        {
            _timesheetRepoMock = new Mock<IRepository<int, Timesheet>>();
            _overtimeRuleRepoMock = new Mock<IRepository<int, OvertimeRule>>();

            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TimeSheetContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _service = new TimesheetService(_timesheetRepoMock.Object, _overtimeRuleRepoMock.Object, _context, _mapper);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task CreateTimesheet_ValidEntry_ReturnsTimesheetResponse()
        {
            // Arrange
            var employee = new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com" };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var dto = new TimesheetCreateDto { Date = DateTime.UtcNow.Date, HoursWorked = 8, Comments = "Work done" };
            _timesheetRepoMock.Setup(r => r.Add(It.IsAny<Timesheet>())).ReturnsAsync((Timesheet t) => { t.TimesheetId = 1; return t; });

            // Act
            var result = await _service.CreateTimesheet(1, dto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.EmployeeId, Is.EqualTo(1));
            Assert.That(result.HoursWorked, Is.EqualTo(8));
        }

        [Test]
        public async Task CreateTimesheet_DuplicateDate_ThrowsDuplicateEntityException()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            _context.Timesheets.Add(new Timesheet { TimesheetId = 1, EmployeeId = 1, Date = today, HoursWorked = 8 });
            await _context.SaveChangesAsync();

            var dto = new TimesheetCreateDto { Date = today, HoursWorked = 7 };

            // Act & Assert
            Assert.ThrowsAsync<DuplicateEntityException>(() => _service.CreateTimesheet(1, dto));
        }

        [Test]
        public async Task CreateTimesheet_MoreThan8Hours_CalculatesOvertime()
        {
            // Arrange
            var employee = new Employee { EmployeeId = 2, Username = "jane", FirstName = "Jane", LastName = "Doe", Email = "jane@t.com" };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var dto = new TimesheetCreateDto { Date = DateTime.UtcNow.Date.AddDays(1), HoursWorked = 10 };

            Timesheet capturedTimesheet = null!;
            _timesheetRepoMock.Setup(r => r.Add(It.IsAny<Timesheet>()))
                .Callback<Timesheet>(t => capturedTimesheet = t)
                .ReturnsAsync((Timesheet t) => { t.TimesheetId = 2; return t; });

            // Act
            await _service.CreateTimesheet(2, dto);

            // Assert
            Assert.That(capturedTimesheet.OvertimeHours, Is.EqualTo(2));
        }

        [Test]
        public async Task CreateTimesheet_WithActiveOvertimeRule_UsesRuleForCalc()
        {
            // Arrange
            var employee = new Employee { EmployeeId = 3, Username = "bob", FirstName = "Bob", LastName = "Smith", Email = "bob@t.com" };
            var rule = new OvertimeRule { OvertimeRuleId = 1, RuleName = "Custom", MaxRegularHours = 6, OvertimeMultiplier = 2, EffectiveFrom = DateTime.UtcNow.AddDays(-10), IsActive = true };
            _context.Employees.Add(employee);
            _context.OvertimeRules.Add(rule);
            await _context.SaveChangesAsync();

            var dto = new TimesheetCreateDto { Date = DateTime.UtcNow.Date.AddDays(2), HoursWorked = 9 };

            Timesheet capturedTimesheet = null!;
            _timesheetRepoMock.Setup(r => r.Add(It.IsAny<Timesheet>()))
                .Callback<Timesheet>(t => capturedTimesheet = t)
                .ReturnsAsync((Timesheet t) => { t.TimesheetId = 3; return t; });

            // Act
            await _service.CreateTimesheet(3, dto);

            // Assert — 9 - 6 = 3 hours overtime (using custom rule maxRegularHours=6)
            Assert.That(capturedTimesheet.OvertimeHours, Is.EqualTo(3));
        }

        [Test]
        public void UpdateTimesheet_NotOwner_ThrowsUnAuthorizedException()
        {
            // Arrange
            var timesheet = new Timesheet { TimesheetId = 1, EmployeeId = 1, Status = TimesheetStatus.Pending };
            _timesheetRepoMock.Setup(r => r.Get(1)).ReturnsAsync(timesheet);

            // Act & Assert — employee 2 trying to update employee 1's timesheet
            Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _service.UpdateTimesheet(1, 2, new TimesheetUpdateDto { HoursWorked = 7 }));
        }

        [Test]
        public void UpdateTimesheet_AlreadyApproved_ThrowsValidationException()
        {
            // Arrange
            var timesheet = new Timesheet { TimesheetId = 1, EmployeeId = 1, Status = TimesheetStatus.Approved };
            _timesheetRepoMock.Setup(r => r.Get(1)).ReturnsAsync(timesheet);

            // Act & Assert
            Assert.ThrowsAsync<Exceptions.ValidationException>(() =>
                _service.UpdateTimesheet(1, 1, new TimesheetUpdateDto { HoursWorked = 7 }));
        }

        [Test]
        public async Task ApproveTimesheet_PendingTimesheet_StatusChangesToApproved()
        {
            // Arrange
            var timesheet = new Timesheet { TimesheetId = 1, EmployeeId = 1, Status = TimesheetStatus.Pending, Date = DateTime.UtcNow };
            var employee = new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com" };
            _context.Employees.Add(employee);
            _context.Timesheets.Add(timesheet);
            await _context.SaveChangesAsync();

            _timesheetRepoMock.Setup(r => r.Get(1)).ReturnsAsync(timesheet);
            _timesheetRepoMock.Setup(r => r.Update(It.IsAny<Timesheet>())).ReturnsAsync((Timesheet t) => t);

            // Act
            var result = await _service.ApproveTimesheet(1, "admin");

            // Assert
            Assert.That(result.Status, Is.EqualTo("Approved"));
        }

        [Test]
        public void RejectTimesheet_AlreadyApproved_ThrowsValidationException()
        {
            // Arrange
            var timesheet = new Timesheet { TimesheetId = 1, EmployeeId = 1, Status = TimesheetStatus.Approved };
            _timesheetRepoMock.Setup(r => r.Get(1)).ReturnsAsync(timesheet);

            // Act & Assert
            Assert.ThrowsAsync<Exceptions.ValidationException>(() => _service.RejectTimesheet(1, "admin"));
        }
    }
}
