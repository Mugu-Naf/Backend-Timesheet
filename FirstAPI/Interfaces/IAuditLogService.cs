using FirstAPI.Models.DTOs;

namespace FirstAPI.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string username, string action, string entityType,
                      int? entityId = null, string? description = null,
                      string? ipAddress = null, string status = "Success");

        Task<AuditLogPagedResponseDto> GetLogsAsync(AuditLogFilterDto filter);
        Task<IEnumerable<AuditLogResponseDto>> GetRecentLogsAsync(int count = 10);
    }
}
