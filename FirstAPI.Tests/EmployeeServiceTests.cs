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
    public class EmployeeServiceTests : IDisposable
    {
        private readonly TimeSheetContext _context;
        private readonly Mock<IRepository<int, Employee>> _employeeRepoMock;
        private readonly IMapper _mapper;
        private readonly EmployeeService _service;

        public EmployeeServiceTests()
        {
            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new TimeSheetContext(options);

            _employeeRepoMock = new Mock<IRepository<int, Employee>>();

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _service = new EmployeeService(
                _employeeRepoMock.Object,
                _context,
                _mapper);
        }

        public void Dispose() => _context.Dispose();

        // ── GetEmployeeByUsername Tests ──────────────────────────────

        [Fact]
        public async Task GetEmployeeByUsername_ValidUsername_ReturnsEmployee()
        {
            // Arrange
            _context.Employees.Add(new Employee
            {
                EmployeeId    = 1,
                Username      = "john",
                FirstName     = "John",
                LastName      = "Doe",
                Email         = "john@test.com",
                DateOfJoining = DateTime.Today
            });
            await _context.SaveChangesAsync();

            _employeeRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Employees);

            // Act
            var result = await _service.GetEmployeeByUsername("john");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John", result.FirstName);
            Assert.Equal("Doe", result.LastName);
        }

        [Fact]
        public async Task GetEmployeeByUsername_NotFound_ThrowsEntityNotFoundException()
        {
            // Arrange — empty DB
            _employeeRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Employees);

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(
                () => _service.GetEmployeeByUsername("nonexistent"));
        }

        // ── GetEmployeeProfile Tests ─────────────────────────────────

        [Fact]
        public async Task GetEmployeeProfile_ValidId_ReturnsProfile()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId    = 1,
                Username      = "john",
                FirstName     = "John",
                LastName      = "Doe",
                Email         = "john@test.com",
                DateOfJoining = DateTime.Today
            };

            _employeeRepoMock.Setup(r => r.Get(1)).ReturnsAsync(employee);

            // Act
            var result = await _service.GetEmployeeProfile(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John", result.FirstName);
        }

        // ── GetAllEmployees Tests ────────────────────────────────────

        [Fact]
        public async Task GetAllEmployees_ReturnsAllEmployees()
        {
            // Arrange
            var employees = new List<Employee>
            {
                new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com", DateOfJoining = DateTime.Today },
                new Employee { EmployeeId = 2, Username = "jane", FirstName = "Jane", LastName = "Doe", Email = "ja@t.com", DateOfJoining = DateTime.Today }
            };

            _employeeRepoMock.Setup(r => r.GetAll()).ReturnsAsync(employees);

            // Act
            var result = await _service.GetAllEmployees();

            // Assert
            Assert.Equal(2, result.Count());
        }

        // ── UpdateEmployee Tests ─────────────────────────────────────

        [Fact]
        public async Task UpdateEmployee_ValidData_UpdatesFields()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId    = 1,
                Username      = "john",
                FirstName     = "John",
                LastName      = "Doe",
                Email         = "john@test.com",
                DateOfJoining = DateTime.Today
            };

            _employeeRepoMock.Setup(r => r.Get(1)).ReturnsAsync(employee);
            _employeeRepoMock.Setup(r => r.Update(It.IsAny<Employee>()))
                .ReturnsAsync((Employee e) => e);

            var dto = new EmployeeUpdateDto
            {
                FirstName  = "Johnny",
                LastName   = "Smith",
                Email      = "johnny@test.com",
                Department = "Engineering",
                JobTitle   = "Senior Developer"
            };

            // Act
            var result = await _service.UpdateEmployee(1, dto);

            // Assert
            Assert.Equal("Johnny", result.FirstName);
            Assert.Equal("Smith", result.LastName);
            Assert.Equal("Engineering", result.Department);
        }

        // ── DeleteEmployee Tests ─────────────────────────────────────

        [Fact]
        public async Task DeleteEmployee_ValidId_ReturnsDeletedEmployee()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId    = 1,
                Username      = "john",
                FirstName     = "John",
                LastName      = "Doe",
                Email         = "john@test.com",
                DateOfJoining = DateTime.Today
            };

            _employeeRepoMock.Setup(r => r.Delete(1)).ReturnsAsync(employee);

            // Act
            var result = await _service.DeleteEmployee(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John", result.FirstName);
            _employeeRepoMock.Verify(r => r.Delete(1), Times.Once);
        }
    }
}
