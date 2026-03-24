using FirstAPI.Interfaces;
using FirstAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirstAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Get paginated audit logs with optional filters.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<AuditLogPagedResponseDto>> GetLogs([FromQuery] AuditLogFilterDto filter)
        {
            var result = await _auditLogService.GetLogsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Get the most recent N audit log entries (for dashboard widget).
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<AuditLogResponseDto>>> GetRecent([FromQuery] int count = 10)
        {
            var result = await _auditLogService.GetRecentLogsAsync(count);
            return Ok(result);
        }
    }
}
