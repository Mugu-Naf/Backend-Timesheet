using System.Security.Cryptography;
using AutoMapper;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;

namespace FirstAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<string, User> _userRepository;
        private readonly IRepository<int, Employee> _employeeRepository;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public AuthService(
            IRepository<string, User> userRepository,
            IRepository<int, Employee> employeeRepository,
            IPasswordService passwordService,
            ITokenService tokenService,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _employeeRepository = employeeRepository;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        public async Task<LoginResponseDto> Register(RegisterRequestDto request)
        {
            // Check if user already exists
            try
            {
                var existingUser = await _userRepository.Get(request.Username);
                throw new DuplicateEntityException($"User with username '{request.Username}' already exists");
            }
            catch (Exception ex) when (ex is not DuplicateEntityException)
            {
                // User doesn't exist, continue with registration
            }

            // Generate HMAC key and hash password
            using var hmac = new HMACSHA256();
            var passwordHash = hmac.Key;
            var hashedPassword = await _passwordService.HashPassword(request.Password, passwordHash);

            // Create user
            var user = new User
            {
                Username = request.Username,
                Password = hashedPassword,
                PasswordHash = passwordHash,
                Role = "Employee"
            };

            await _userRepository.Add(user);

            // Create employee profile
            var employee = new Employee
            {
                Username = request.Username,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Department = request.Department,
                JobTitle = request.JobTitle,
                DateOfJoining = DateTime.UtcNow
            };

            await _employeeRepository.Add(employee);

            // Generate token
            var token = await _tokenService.GenerateToken(user);

            return new LoginResponseDto
            {
                Username = user.Username,
                Role = user.Role,
                Token = token
            };
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto request)
        {
            User user;
            try
            {
                user = await _userRepository.Get(request.Username);
            }
            catch
            {
                throw new UnAuthorizedException("Invalid username or password");
            }

            var hashedPassword = await _passwordService.HashPassword(request.Password, user.PasswordHash);
            if (!hashedPassword.SequenceEqual(user.Password))
                throw new UnAuthorizedException("Invalid username or password");

            var token = await _tokenService.GenerateToken(user);

            return new LoginResponseDto
            {
                Username = user.Username,
                Role = user.Role,
                Token = token
            };
        }

        public async Task<string> ForgotPassword(ForgotPasswordDto request)
        {
            User user;
            try
            {
                user = await _userRepository.Get(request.Username);
            }
            catch
            {
                throw new EntityNotFoundException($"User with username '{request.Username}' not found");
            }

            // Generate new HMAC key and hash new password
            using var hmac = new HMACSHA256();
            user.PasswordHash = hmac.Key;
            user.Password = await _passwordService.HashPassword(request.NewPassword, user.PasswordHash);

            await _userRepository.Update(user);

            return "Password has been reset successfully";
        }
    }
}
