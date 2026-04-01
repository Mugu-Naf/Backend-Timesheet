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
    public class AuthServiceTests
    {
        private readonly Mock<IRepository<string, User>> _userRepoMock;
        private readonly Mock<IRepository<int, Employee>> _employeeRepoMock;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly IMapper _mapper;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _userRepoMock         = new Mock<IRepository<string, User>>();
            _employeeRepoMock     = new Mock<IRepository<int, Employee>>();
            _passwordServiceMock  = new Mock<IPasswordService>();
            _tokenServiceMock     = new Mock<ITokenService>();

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _service = new AuthService(
                _userRepoMock.Object,
                _employeeRepoMock.Object,
                _passwordServiceMock.Object,
                _tokenServiceMock.Object,
                _mapper);
        }

        // ── Register Tests ───────────────────────────────────────────

        [Fact]
        public async Task Register_NewUser_ReturnsTokenResponse()
        {
            // Arrange
            _userRepoMock.Setup(r => r.Get(It.IsAny<string>()))
                .ThrowsAsync(new Exception("not found")); // user doesn't exist

            _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            _userRepoMock.Setup(r => r.Add(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);

            _employeeRepoMock.Setup(r => r.Add(It.IsAny<Employee>()))
                .ReturnsAsync((Employee e) => e);

            _tokenServiceMock.Setup(t => t.GenerateToken(It.IsAny<User>()))
                .ReturnsAsync("fake-jwt-token");

            var request = new RegisterRequestDto
            {
                Username  = "newuser",
                Password  = "Password123",
                FirstName = "John",
                LastName  = "Doe",
                Email     = "john@test.com"
            };

            // Act
            var result = await _service.Register(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("newuser", result.Username);
            Assert.Equal("Employee", result.Role);
            Assert.Equal("fake-jwt-token", result.Token);
        }

        [Fact]
        public async Task Register_ExistingUsername_ThrowsDuplicateEntityException()
        {
            // Arrange — user already exists
            _userRepoMock.Setup(r => r.Get("existinguser"))
                .ReturnsAsync(new User { Username = "existinguser", Role = "Employee" });

            var request = new RegisterRequestDto
            {
                Username  = "existinguser",
                Password  = "Password123",
                FirstName = "Jane",
                LastName  = "Doe",
                Email     = "jane@test.com"
            };

            // Act & Assert
            await Assert.ThrowsAsync<DuplicateEntityException>(
                () => _service.Register(request));
        }

        // ── Login Tests ──────────────────────────────────────────────

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            // Arrange
            var passwordBytes = new byte[] { 1, 2, 3 };
            var user = new User
            {
                Username     = "john",
                Password     = passwordBytes,
                PasswordHash = new byte[] { 4, 5, 6 },
                Role         = "Employee"
            };

            _userRepoMock.Setup(r => r.Get("john")).ReturnsAsync(user);

            _passwordServiceMock.Setup(p => p.HashPassword("Password123", user.PasswordHash))
                .ReturnsAsync(passwordBytes); // same as stored = correct password

            _tokenServiceMock.Setup(t => t.GenerateToken(user))
                .ReturnsAsync("valid-token");

            var request = new LoginRequestDto
            {
                Username = "john",
                Password = "Password123"
            };

            // Act
            var result = await _service.Login(request);

            // Assert
            Assert.Equal("valid-token", result.Token);
            Assert.Equal("john", result.Username);
        }

        [Fact]
        public async Task Login_UserNotFound_ThrowsUnAuthorizedException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.Get("unknown"))
                .ThrowsAsync(new Exception("not found"));

            var request = new LoginRequestDto
            {
                Username = "unknown",
                Password = "anypassword"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnAuthorizedException>(
                () => _service.Login(request));
        }

        [Fact]
        public async Task Login_WrongPassword_ThrowsUnAuthorizedException()
        {
            // Arrange
            var storedHash = new byte[] { 1, 2, 3 };
            var user = new User
            {
                Username     = "john",
                Password     = storedHash,
                PasswordHash = new byte[] { 4, 5, 6 },
                Role         = "Employee"
            };

            _userRepoMock.Setup(r => r.Get("john")).ReturnsAsync(user);

            // Wrong password produces different hash
            _passwordServiceMock.Setup(p => p.HashPassword("WrongPassword", user.PasswordHash))
                .ReturnsAsync(new byte[] { 9, 9, 9 }); // different from stored

            var request = new LoginRequestDto
            {
                Username = "john",
                Password = "WrongPassword"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnAuthorizedException>(
                () => _service.Login(request));
        }

        // ── ForgotPassword Tests ─────────────────────────────────────

        [Fact]
        public async Task ForgotPassword_ValidUser_ReturnsSuccessMessage()
        {
            // Arrange
            var user = new User
            {
                Username     = "john",
                Password     = new byte[] { 1, 2, 3 },
                PasswordHash = new byte[] { 4, 5, 6 },
                Role         = "Employee"
            };

            _userRepoMock.Setup(r => r.Get("john")).ReturnsAsync(user);

            _passwordServiceMock.Setup(p => p.HashPassword("NewPassword123", It.IsAny<byte[]>()))
                .ReturnsAsync(new byte[] { 7, 8, 9 });

            _userRepoMock.Setup(r => r.Update(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);

            var request = new ForgotPasswordDto
            {
                Username    = "john",
                NewPassword = "NewPassword123"
            };

            // Act
            var result = await _service.ForgotPassword(request);

            // Assert
            Assert.Contains("successfully", result.ToLower());
            _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_UserNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.Get("unknown"))
                .ThrowsAsync(new Exception("not found"));

            var request = new ForgotPasswordDto
            {
                Username    = "unknown",
                NewPassword = "NewPassword123"
            };

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(
                () => _service.ForgotPassword(request));
        }
    }
}
