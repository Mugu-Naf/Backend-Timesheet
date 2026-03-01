using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Models;

namespace FirstAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<string, User> _userRepository;
        private readonly IPasswordService _passwordService;

        public UserService(IRepository<string, User> userRepository, IPasswordService passwordService)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
        }

        public async Task<User> Authenticate(string username, string password)
        {
            var user = await _userRepository.Get(username);
            if (user == null)
                throw new UnAuthorizedException("Invalid username or password");

            var userPasswordHash = await _passwordService.HashPassword(password, user.PasswordHash);
            if (!userPasswordHash.SequenceEqual(user.Password))
                throw new UnAuthorizedException("Invalid username or password");

            return user;
        }
    }
}
