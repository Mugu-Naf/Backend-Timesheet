using System.Security.Cryptography;
using System.Text;
using FirstAPI.Interfaces;

namespace FirstAPI.Services
{
    public class PasswordService : IPasswordService
    {
        public async Task<byte[]> HashPassword(string password, byte[] key)
        {
            using var hmac = new HMACSHA256(key);
            return await Task.FromResult(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        public async Task<bool> VerifyPassword(string password, byte[] hashedPassword, byte[] key)
        {
            var computedHash = await HashPassword(password, key);
            return computedHash.SequenceEqual(hashedPassword);
        }
    }
}
