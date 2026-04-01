using FirstAPI.Contexts;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using FirstAPI.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using AutoMapper;

namespace FirstAPI.Tests
{
    public class AttendanceServiceTests : IDisposable
    {
        private readonly TimeSheetContext _context;
        private readonly Mock<IRepository<int, Attendance>> _attendanceRepoMock;
        private readonly Mock<IRepository<int, Employee>> _employeeRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly AttendanceService _service;

        public AttendanceServiceTests()
        {
            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new TimeSheetContext(options);

            _attendanceRepoMock = new Mock<IRepository<int, Attendance>>();
            _employeeRepoMock   = new Mock<IRepository<int, Employee>>();
            _mapperMock         = new Mock<IMapper>();

            _service = new AttendanceService(
                _attendanceRepoMock.Object,
                _employeeRepoMock.Object,
                _mapperMock.Object);
        }

        public void Dispose() => _context.Dispose();

        // ── CheckIn Tests ────────────────────────────────────────────

        [Fact]
        public async Task CheckIn_NoExistingSession_CreatesNewRecord()
        {
            // Arrange — no existing attendance records
            _attendanceRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Attendances);

            _attendanceRepoMock.Setup(r => r.Add(It.IsAny<Attendance>()))
                .ReturnsAsync((Attendance a) => { a.AttendanceId = 1; return a; });

            var dto = new AttendanceCheckInDto();

            // Act
            var result = await _service.CheckIn(1, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.EmployeeId);
            _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Once);
        }

        [Fact]
        public async Task CheckIn_OpenSessionToday_ThrowsValidationException()
        {
            // Arrange — there is already an open session today
            var istOffset = TimeSpan.FromHours(5.5);
            var todayIst  = DateTime.UtcNow.Add(istOffset).Date;

            _context.Attendances.Add(new Attendance
            {
                AttendanceId = 1,
                EmployeeId   = 1,
                Date         = todayIst,
                CheckInTime  = todayIst.AddHours(9),
                CheckOutTime = null,  // open session
                Status       = AttendanceStatus.Present
            });
            await _context.SaveChangesAsync();

            _attendanceRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Attendances);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.CheckIn(1, new AttendanceCheckInDto()));
        }

        [Fact]
        public async Task CheckIn_ForgotCheckoutPreviousDay_ThrowsValidationException()
        {
            // Arrange — open session from yesterday
            var istOffset   = TimeSpan.FromHours(5.5);
            var yesterdayIst = DateTime.UtcNow.Add(istOffset).Date.AddDays(-1);

            _context.Attendances.Add(new Attendance
            {
                AttendanceId = 2,
                EmployeeId   = 1,
                Date         = yesterdayIst,
                CheckInTime  = yesterdayIst.AddHours(9),
                CheckOutTime = null,  // forgot to checkout
                Status       = AttendanceStatus.Present
            });
            await _context.SaveChangesAsync();

            _attendanceRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Attendances);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.CheckIn(1, new AttendanceCheckInDto()));
        }

        // ── CheckOut Tests ───────────────────────────────────────────

        [Fact]
        public async Task CheckOut_OpenSessionExists_SetsCheckoutTime()
        {
            // Arrange
            var istOffset = TimeSpan.FromHours(5.5);
            var todayIst  = DateTime.UtcNow.Add(istOffset).Date;

            var attendance = new Attendance
            {
                AttendanceId = 1,
                EmployeeId   = 1,
                Date         = todayIst,
                CheckInTime  = todayIst.AddHours(9),
                CheckOutTime = null,
                Status       = AttendanceStatus.Present
            };
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            _attendanceRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Attendances);

            _attendanceRepoMock.Setup(r => r.Update(It.IsAny<Attendance>()))
                .ReturnsAsync((Attendance a) => a);

            // Act
            var result = await _service.CheckOut(1, new AttendanceCheckOutDto());

            // Assert
            Assert.NotNull(result);
            _attendanceRepoMock.Verify(r => r.Update(It.IsAny<Attendance>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task CheckOut_NoOpenSession_ThrowsEntityNotFoundException()
        {
            // Arrange — no open session today
            _attendanceRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Attendances);

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(
                () => _service.CheckOut(1, new AttendanceCheckOutDto()));
        }

        [Fact]
        public async Task CheckOut_LessThan4Hours_StatusIsHalfDay()
        {
            // Arrange — check in 1 hour ago
            var istOffset = TimeSpan.FromHours(5.5);
            var nowIst    = DateTime.UtcNow.Add(istOffset);
            var todayIst  = nowIst.Date;

            var attendance = new Attendance
            {
                AttendanceId = 1,
                EmployeeId   = 1,
                Date         = todayIst,
                CheckInTime  = nowIst.AddHours(-1), // only 1 hour ago
                CheckOutTime = null,
                Status       = AttendanceStatus.Present
            };
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            _attendanceRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Attendances);

            _attendanceRepoMock.Setup(r => r.Update(It.IsAny<Attendance>()))
                .ReturnsAsync((Attendance a) => a);

            // Act
            var result = await _service.CheckOut(1, new AttendanceCheckOutDto());

            // Assert — less than 4 hours = HalfDay
            Assert.Equal("HalfDay", result.Status);
        }

        [Fact]
        public async Task CheckOut_MoreThan4Hours_StatusIsPresent()
        {
            // Arrange — check in 5 hours ago
            var istOffset = TimeSpan.FromHours(5.5);
            var nowIst    = DateTime.UtcNow.Add(istOffset);
            var todayIst  = nowIst.Date;

            var attendance = new Attendance
            {
                AttendanceId = 1,
                EmployeeId   = 1,
                Date         = todayIst,
                CheckInTime  = nowIst.AddHours(-5), // 5 hours ago
                CheckOutTime = null,
                Status       = AttendanceStatus.Present
            };
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            _attendanceRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Attendances);

            _attendanceRepoMock.Setup(r => r.Update(It.IsAny<Attendance>()))
                .ReturnsAsync((Attendance a) => a);

            // Act
            var result = await _service.CheckOut(1, new AttendanceCheckOutDto());

            // Assert — more than 4 hours = Present
            Assert.Equal("Present", result.Status);
        }

        // ── FixCheckout Tests ────────────────────────────────────────

        [Fact]
        public async Task FixCheckout_OpenSession_SetsCheckoutTime()
        {
            // Arrange
            var attendance = new Attendance
            {
                AttendanceId = 5,
                EmployeeId   = 1,
                Date         = DateTime.Today.AddDays(-1),
                CheckInTime  = DateTime.Today.AddDays(-1).AddHours(9),
                CheckOutTime = null,
                Status       = AttendanceStatus.Present
            };
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            _attendanceRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Attendances);

            _attendanceRepoMock.Setup(r => r.Update(It.IsAny<Attendance>()))
                .ReturnsAsync((Attendance a) => a);

            var dto = new AttendanceCheckOutDto
            {
                CheckOutTime = DateTime.Today.AddDays(-1).AddHours(17)
            };

            // Act
            var result = await _service.FixCheckout(5, dto);

            // Assert
            Assert.NotNull(result);
            _attendanceRepoMock.Verify(r => r.Update(It.IsAny<Attendance>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task FixCheckout_AlreadyCheckedOut_ThrowsValidationException()
        {
            // The FixCheckout service uses Include(Employee) which InMemory DB
            // doesn't support for navigation. Test the "not found" path instead
            // and verify ValidationException is thrown when session has checkout.
            // We test this via the service's own validation logic.

            // Arrange — use a fresh context with the record
            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var ctx = new TimeSheetContext(options);

            var attendance = new Attendance
            {
                EmployeeId   = 1,
                Date         = DateTime.Today.AddDays(-1),
                CheckInTime  = DateTime.Today.AddDays(-1).AddHours(9),
                CheckOutTime = DateTime.Today.AddDays(-1).AddHours(17),
                Status       = AttendanceStatus.Present
            };
            ctx.Attendances.Add(attendance);
            await ctx.SaveChangesAsync();
            int actualId = attendance.AttendanceId;

            var repoMock = new Mock<IRepository<int, Attendance>>();
            repoMock.Setup(r => r.GetQueryable()).Returns(ctx.Attendances);
            repoMock.Setup(r => r.Update(It.IsAny<Attendance>()))
                .ReturnsAsync((Attendance a) => a);

            var svc = new AttendanceService(repoMock.Object, _employeeRepoMock.Object, _mapperMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => svc.FixCheckout(actualId, new AttendanceCheckOutDto()));
        }

        [Fact]
        public async Task FixCheckout_RecordNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange — empty DB, mock returns empty queryable
            _attendanceRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Attendances); // empty

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(
                () => _service.FixCheckout(999, new AttendanceCheckOutDto()));
        }
    }
}
