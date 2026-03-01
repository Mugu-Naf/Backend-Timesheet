namespace FirstAPI.Interfaces
{
    public interface IPasswordService
    {
        Task<byte[]> HashPassword(string password, byte[] key);
        Task<bool> VerifyPassword(string password, byte[] hashedPassword, byte[] key);
    }
}
