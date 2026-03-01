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
    public class EmployeeServiceTests
    {
        private Mock<IRepository<int, Employee>> _employeeRepoMock;
        private TimeSheetContext _context;
        private IMapper _mapper;
        private EmployeeService _service;

        [SetUp]
        public void Setup()
        {
            _employeeRepoMock = new Mock<IRepository<int, Employee>>();

            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TimeSheetContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _service = new EmployeeService(_employeeRepoMock.Object, _context, _mapper);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task GetEmployeeProfile_Exists_ReturnsProfile()
        {
            // Arrange
            var employee = new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com", Department = "IT" };
            _employeeRepoMock.Setup(r => r.Get(1)).ReturnsAsync(employee);

            // Act
            var result = await _service.GetEmployeeProfile(1);

            // Assert
            Assert.That(result.FirstName, Is.EqualTo("John"));
            Assert.That(result.Department, Is.EqualTo("IT"));
        }

        [Test]
        public async Task GetEmployeeByUsername_Exists_ReturnsProfile()
        {
            // Arrange
            _context.Employees.Add(new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com" });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetEmployeeByUsername("john");

            // Assert
            Assert.That(result.Username, Is.EqualTo("john"));
        }

        [Test]
        public void GetEmployeeByUsername_NotFound_ThrowsEntityNotFoundException()
        {
            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(() => _service.GetEmployeeByUsername("nouser"));
        }

        [Test]
        public async Task GetAllEmployees_ReturnsAll()
        {
            // Arrange
            var employees = new List<Employee>
            {
                new Employee { EmployeeId = 1, Username = "a", FirstName = "A", LastName = "A", Email = "a@t.com" },
                new Employee { EmployeeId = 2, Username = "b", FirstName = "B", LastName = "B", Email = "b@t.com" }
            };
            _employeeRepoMock.Setup(r => r.GetAll()).ReturnsAsync(employees);

            // Act
            var result = await _service.GetAllEmployees();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task UpdateEmployee_Valid_ReturnsUpdated()
        {
            // Arrange
            var employee = new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com" };
            _employeeRepoMock.Setup(r => r.Get(1)).ReturnsAsync(employee);
            _employeeRepoMock.Setup(r => r.Update(It.IsAny<Employee>())).ReturnsAsync((Employee e) => e);

            var dto = new EmployeeUpdateDto { FirstName = "Jane", LastName = "Smith", Email = "jane@t.com", Department = "HR" };

            // Act
            var result = await _service.UpdateEmployee(1, dto);

            // Assert
            Assert.That(result.FirstName, Is.EqualTo("Jane"));
            Assert.That(result.Department, Is.EqualTo("HR"));
        }

        [Test]
        public async Task DeleteEmployee_Valid_ReturnsDeleted()
        {
            // Arrange
            var employee = new Employee { EmployeeId = 1, Username = "john", FirstName = "John", LastName = "Doe", Email = "j@t.com" };
            _employeeRepoMock.Setup(r => r.Delete(1)).ReturnsAsync(employee);

            // Act
            var result = await _service.DeleteEmployee(1);

            // Assert
            Assert.That(result.FirstName, Is.EqualTo("John"));
        }
    }
}
