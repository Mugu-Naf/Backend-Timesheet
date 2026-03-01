using FirstAPI.Models;

namespace FirstAPI.Interfaces
{
    public interface IUserService
    {
        Task<User> Authenticate(string username, string password);
    }
}
