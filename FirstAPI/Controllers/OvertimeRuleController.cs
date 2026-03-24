using System.Security.Claims;
using FirstAPI.Interfaces;
using FirstAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirstAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "HR,Admin")]
    public class OvertimeRuleController : ControllerBase
    {
        private readonly IOvertimeRuleService _overtimeRuleService;
        private readonly IAuditLogService _auditLog;

        public OvertimeRuleController(IOvertimeRuleService overtimeRuleService, IAuditLogService auditLog)
        {
            _overtimeRuleService = overtimeRuleService;
            _auditLog            = auditLog;
        }

        private string GetUsername() => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpPost]
        public async Task<ActionResult<OvertimeRuleResponseDto>> Create([FromBody] OvertimeRuleCreateDto dto)
        {
            var result = await _overtimeRuleService.CreateRule(dto);
            await _auditLog.LogAsync(GetUsername(), "CREATE", "OvertimeRule", result.OvertimeRuleId,
                $"Created overtime rule '{result.RuleName}'", GetIp());
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<OvertimeRuleResponseDto>> Update(int id, [FromBody] OvertimeRuleUpdateDto dto)
        {
            var result = await _overtimeRuleService.UpdateRule(id, dto);
            await _auditLog.LogAsync(GetUsername(), "UPDATE", "OvertimeRule", id,
                $"Updated overtime rule '{result.RuleName}'", GetIp());
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<OvertimeRuleResponseDto>> Delete(int id)
        {
            var result = await _overtimeRuleService.DeleteRule(id);
            await _auditLog.LogAsync(GetUsername(), "DELETE", "OvertimeRule", id,
                $"Deleted overtime rule #{id}", GetIp());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OvertimeRuleResponseDto>> GetById(int id)
        {
            var result = await _overtimeRuleService.GetRuleById(id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OvertimeRuleResponseDto>>> GetAll()
        {
            var result = await _overtimeRuleService.GetAllRules();
            return Ok(result);
        }

        [HttpGet("active")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<OvertimeRuleResponseDto>> GetActive()
        {
            var result = await _overtimeRuleService.GetActiveRule();
            if (result == null)
                throw new Exceptions.EntityNotFoundException("No active overtime rule found");
            return Ok(result);
        }
    }
}
