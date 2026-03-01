using System.Security.Cryptography;
using AutoMapper;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Mappings;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using FirstAPI.Services;
using Moq;

namespace FirstAPI.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IRepository<string, User>> _userRepoMock;
        private Mock<IRepository<int, Employee>> _employeeRepoMock;
        private Mock<IPasswordService> _passwordServiceMock;
        private Mock<ITokenService> _tokenServiceMock;
        private IMapper _mapper;
        private AuthService _authService;

        [SetUp]
        public void Setup()
        {
            _userRepoMock = new Mock<IRepository<string, User>>();
            _employeeRepoMock = new Mock<IRepository<int, Employee>>();
            _passwordServiceMock = new Mock<IPasswordService>();
            _tokenServiceMock = new Mock<ITokenService>();

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _authService = new AuthService(
                _userRepoMock.Object,
                _employeeRepoMock.Object,
                _passwordServiceMock.Object,
                _tokenServiceMock.Object,
                _mapper);
        }

        [Test]
        public async Task Register_ValidRequest_ReturnsLoginResponse()
        {
            // Arrange
            var request = new RegisterRequestDto
            {
                Username = "testuser",
                Password = "Test@123",
                Role = "Employee",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@test.com"
            };

            _userRepoMock.Setup(r => r.Get("testuser")).ThrowsAsync(new Exception("Not found"));
            _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _userRepoMock.Setup(r => r.Add(It.IsAny<User>())).ReturnsAsync((User u) => u);
            _employeeRepoMock.Setup(r => r.Add(It.IsAny<Employee>())).ReturnsAsync((Employee e) => e);
            _tokenServiceMock.Setup(t => t.GenerateToken(It.IsAny<User>())).ReturnsAsync("mock-jwt-token");

            // Act
            var result = await _authService.Register(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Username, Is.EqualTo("testuser"));
            Assert.That(result.Token, Is.EqualTo("mock-jwt-token"));
            Assert.That(result.Role, Is.EqualTo("Employee"));
        }

        [Test]
        public void Register_DuplicateUser_ThrowsDuplicateEntityException()
        {
            // Arrange
            var request = new RegisterRequestDto { Username = "existing", Password = "Test@123", FirstName = "A", LastName = "B", Email = "a@b.com" };
            _userRepoMock.Setup(r => r.Get("existing")).ReturnsAsync(new User { Username = "existing" });

            // Act & Assert
            Assert.ThrowsAsync<DuplicateEntityException>(() => _authService.Register(request));
        }

        [Test]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            // Arrange
            var hashedPassword = new byte[] { 1, 2, 3 };
            var user = new User { Username = "testuser", Password = hashedPassword, PasswordHash = new byte[] { 4, 5, 6 }, Role = "Employee" };

            _userRepoMock.Setup(r => r.Get("testuser")).ReturnsAsync(user);
            _passwordServiceMock.Setup(p => p.HashPassword("Test@123", user.PasswordHash)).ReturnsAsync(hashedPassword);
            _tokenServiceMock.Setup(t => t.GenerateToken(user)).ReturnsAsync("jwt-token");

            // Act
            var result = await _authService.Login(new LoginRequestDto { Username = "testuser", Password = "Test@123" });

            // Assert
            Assert.That(result.Token, Is.EqualTo("jwt-token"));
            Assert.That(result.Username, Is.EqualTo("testuser"));
        }

        [Test]
        public void Login_InvalidPassword_ThrowsUnAuthorizedException()
        {
            // Arrange
            var user = new User { Username = "testuser", Password = new byte[] { 1, 2, 3 }, PasswordHash = new byte[] { 4, 5, 6 } };
            _userRepoMock.Setup(r => r.Get("testuser")).ReturnsAsync(user);
            _passwordServiceMock.Setup(p => p.HashPassword("wrong", user.PasswordHash)).ReturnsAsync(new byte[] { 9, 9, 9 });

            // Act & Assert
            Assert.ThrowsAsync<UnAuthorizedException>(() => _authService.Login(new LoginRequestDto { Username = "testuser", Password = "wrong" }));
        }

        [Test]
        public void Login_UserNotFound_ThrowsUnAuthorizedException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.Get("nouser")).ThrowsAsync(new Exception("Not found"));

            // Act & Assert
            Assert.ThrowsAsync<UnAuthorizedException>(() => _authService.Login(new LoginRequestDto { Username = "nouser", Password = "any" }));
        }

        [Test]
        public async Task ForgotPassword_ValidUser_ResetsPassword()
        {
            // Arrange
            var user = new User { Username = "testuser", Password = new byte[] { 1 }, PasswordHash = new byte[] { 2 } };
            _userRepoMock.Setup(r => r.Get("testuser")).ReturnsAsync(user);
            _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>()))
                .ReturnsAsync(new byte[] { 7, 8, 9 });
            _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).ReturnsAsync((User u) => u);

            // Act
            var result = await _authService.ForgotPassword(new ForgotPasswordDto { Username = "testuser", NewPassword = "New@123" });

            // Assert
            Assert.That(result, Does.Contain("successfully"));
            _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
        }

        [Test]
        public void ForgotPassword_UserNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.Get("nouser")).ThrowsAsync(new Exception("Not found"));

            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _authService.ForgotPassword(new ForgotPasswordDto { Username = "nouser", NewPassword = "New@123" }));
        }
    }
}
