using FirstAPI.Interfaces;
using FirstAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FirstAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLog;

        public AuthController(IAuthService authService, IAuditLogService auditLog)
        {
            _authService = authService;
            _auditLog    = auditLog;
        }

        private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            var result = await _authService.Register(request);
            await _auditLog.LogAsync(request.Username, "REGISTER", "User", null,
                "New user registered", GetIp());
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.Login(request);
            await _auditLog.LogAsync(request.Username, "LOGIN", "User", null,
                "User logged in", GetIp());
            return Ok(result);
        }

        [HttpPut("forgot-password")]
        public async Task<ActionResult<string>> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            var result = await _authService.ForgotPassword(request);
            await _auditLog.LogAsync(request.Username, "PASSWORD_RESET", "User", null,
                "Password reset requested", GetIp());
            return Ok(new { message = result });
        }
    }
}
