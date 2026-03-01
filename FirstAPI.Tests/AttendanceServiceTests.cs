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
    public class AttendanceServiceTests
    {
        private Mock<IRepository<int, Attendance>> _attendanceRepoMock;
        private TimeSheetContext _context;
        private IMapper _mapper;
        private AttendanceService _service;

        [SetUp]
        public void Setup()
        {
            _attendanceRepoMock = new Mock<IRepository<int, Attendance>>();

            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TimeSheetContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _service = new AttendanceService(_attendanceRepoMock.Object, _context, _mapper);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task CheckIn_FirstTimeToday_ReturnsAttendanceResponse()
        {
            // Arrange
            var employee = new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com" };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            _attendanceRepoMock.Setup(r => r.Add(It.IsAny<Attendance>()))
                .ReturnsAsync((Attendance a) => { a.AttendanceId = 1; return a; });

            // Act
            var result = await _service.CheckIn(1, new AttendanceCheckInDto());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.EmployeeId, Is.EqualTo(1));
            Assert.That(result.Status, Is.EqualTo("Present"));
        }

        [Test]
        public async Task CheckIn_AlreadyCheckedIn_ThrowsDuplicateEntityException()
        {
            // Arrange
            _context.Attendances.Add(new Attendance
            {
                AttendanceId = 1,
                EmployeeId = 1,
                Date = DateTime.UtcNow.Date,
                CheckInTime = DateTime.UtcNow,
                Status = AttendanceStatus.Present
            });
            await _context.SaveChangesAsync();

            // Act & Assert
            Assert.ThrowsAsync<DuplicateEntityException>(() => _service.CheckIn(1, new AttendanceCheckInDto()));
        }

        [Test]
        public void CheckOut_NoCheckIn_ThrowsEntityNotFoundException()
        {
            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(() => _service.CheckOut(99, new AttendanceCheckOutDto()));
        }

        [Test]
        public async Task CheckOut_AlreadyCheckedOut_ThrowsValidationException()
        {
            // Arrange
            _context.Attendances.Add(new Attendance
            {
                AttendanceId = 1,
                EmployeeId = 1,
                Date = DateTime.UtcNow.Date,
                CheckInTime = DateTime.UtcNow.AddHours(-8),
                CheckOutTime = DateTime.UtcNow,
                Status = AttendanceStatus.Present
            });
            await _context.SaveChangesAsync();

            // Act & Assert
            Assert.ThrowsAsync<Exceptions.ValidationException>(() =>
                _service.CheckOut(1, new AttendanceCheckOutDto()));
        }

        [Test]
        public async Task GetAttendanceReport_ReturnsCorrectCounts()
        {
            // Arrange
            var employee = new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com" };
            _context.Employees.Add(employee);
            _context.Attendances.AddRange(
                new Attendance { AttendanceId = 1, EmployeeId = 1, Date = DateTime.UtcNow.AddDays(-3), Status = AttendanceStatus.Present },
                new Attendance { AttendanceId = 2, EmployeeId = 1, Date = DateTime.UtcNow.AddDays(-2), Status = AttendanceStatus.Present },
                new Attendance { AttendanceId = 3, EmployeeId = 1, Date = DateTime.UtcNow.AddDays(-1), Status = AttendanceStatus.Absent },
                new Attendance { AttendanceId = 4, EmployeeId = 1, Date = DateTime.UtcNow, Status = AttendanceStatus.HalfDay }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAttendanceReport(1, DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1));

            // Assert
            Assert.That(result.TotalPresent, Is.EqualTo(2));
            Assert.That(result.TotalAbsent, Is.EqualTo(1));
            Assert.That(result.TotalHalfDay, Is.EqualTo(1));
        }

        [Test]
        public void GetAttendanceReport_EmployeeNotFound_ThrowsEntityNotFoundException()
        {
            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _service.GetAttendanceReport(999, DateTime.UtcNow.AddDays(-5), DateTime.UtcNow));
        }
    }
}
