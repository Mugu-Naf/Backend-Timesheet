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
    public class LeaveRequestServiceTests
    {
        private Mock<IRepository<int, LeaveRequest>> _leaveRepoMock;
        private TimeSheetContext _context;
        private IMapper _mapper;
        private LeaveRequestService _service;

        [SetUp]
        public void Setup()
        {
            _leaveRepoMock = new Mock<IRepository<int, LeaveRequest>>();

            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TimeSheetContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _service = new LeaveRequestService(_leaveRepoMock.Object, _context, _mapper);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task CreateLeaveRequest_Valid_ReturnsResponse()
        {
            // Arrange
            var employee = new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com" };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var dto = new LeaveRequestCreateDto
            {
                LeaveType = "Casual",
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(7),
                Reason = "Vacation"
            };

            _leaveRepoMock.Setup(r => r.Add(It.IsAny<LeaveRequest>()))
                .ReturnsAsync((LeaveRequest l) => { l.LeaveRequestId = 1; return l; });

            // Act
            var result = await _service.CreateLeaveRequest(1, dto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.LeaveType, Is.EqualTo("Casual"));
            Assert.That(result.Status, Is.EqualTo("Pending"));
        }

        [Test]
        public void CreateLeaveRequest_EndBeforeStart_ThrowsValidationException()
        {
            // Arrange
            var dto = new LeaveRequestCreateDto
            {
                LeaveType = "Sick",
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(3)
            };

            // Act & Assert
            Assert.ThrowsAsync<Exceptions.ValidationException>(() => _service.CreateLeaveRequest(1, dto));
        }

        [Test]
        public void CreateLeaveRequest_InvalidLeaveType_ThrowsValidationException()
        {
            // Arrange
            var dto = new LeaveRequestCreateDto
            {
                LeaveType = "InvalidType",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2)
            };

            // Act & Assert
            Assert.ThrowsAsync<Exceptions.ValidationException>(() => _service.CreateLeaveRequest(1, dto));
        }

        [Test]
        public async Task CreateLeaveRequest_OverlappingDates_ThrowsDuplicateEntityException()
        {
            // Arrange
            _context.LeaveRequests.Add(new LeaveRequest
            {
                LeaveRequestId = 1,
                EmployeeId = 1,
                LeaveType = LeaveType.Casual,
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(10),
                Status = LeaveStatus.Pending
            });
            await _context.SaveChangesAsync();

            var dto = new LeaveRequestCreateDto
            {
                LeaveType = "Sick",
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(12)
            };

            // Act & Assert
            Assert.ThrowsAsync<DuplicateEntityException>(() => _service.CreateLeaveRequest(1, dto));
        }

        [Test]
        public async Task ApproveLeaveRequest_Pending_StatusChangesToApproved()
        {
            // Arrange
            var leave = new LeaveRequest { LeaveRequestId = 1, EmployeeId = 1, Status = LeaveStatus.Pending, LeaveType = LeaveType.Casual, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
            var employee = new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com" };
            _context.Employees.Add(employee);
            _context.LeaveRequests.Add(leave);
            await _context.SaveChangesAsync();

            _leaveRepoMock.Setup(r => r.Get(1)).ReturnsAsync(leave);
            _leaveRepoMock.Setup(r => r.Update(It.IsAny<LeaveRequest>())).ReturnsAsync((LeaveRequest l) => l);

            // Act
            var result = await _service.ApproveLeaveRequest(1, "admin");

            // Assert
            Assert.That(result.Status, Is.EqualTo("Approved"));
        }

        [Test]
        public void RejectLeaveRequest_AlreadyApproved_ThrowsValidationException()
        {
            // Arrange
            var leave = new LeaveRequest { LeaveRequestId = 1, EmployeeId = 1, Status = LeaveStatus.Approved };
            _leaveRepoMock.Setup(r => r.Get(1)).ReturnsAsync(leave);

            // Act & Assert
            Assert.ThrowsAsync<Exceptions.ValidationException>(() => _service.RejectLeaveRequest(1, "admin"));
        }

        [Test]
        public void CancelLeaveRequest_NotOwner_ThrowsUnAuthorizedException()
        {
            // Arrange
            var leave = new LeaveRequest { LeaveRequestId = 1, EmployeeId = 1, Status = LeaveStatus.Pending };
            _leaveRepoMock.Setup(r => r.Get(1)).ReturnsAsync(leave);

            // Act & Assert — employee 2 trying to cancel employee 1's leave
            Assert.ThrowsAsync<UnAuthorizedException>(() => _service.CancelLeaveRequest(1, 2));
        }
    }
}
