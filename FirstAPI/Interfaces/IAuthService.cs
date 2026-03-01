using FirstAPI.Models.DTOs;

namespace FirstAPI.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> Register(RegisterRequestDto request);
        Task<LoginResponseDto> Login(LoginRequestDto request);
        Task<string> ForgotPassword(ForgotPasswordDto request);
    }
}
