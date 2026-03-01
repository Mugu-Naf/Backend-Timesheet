using FirstAPI.Models;

namespace FirstAPI.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateToken(User user);
    }
}
