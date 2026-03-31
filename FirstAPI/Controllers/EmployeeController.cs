using System.Security.Claims;
using FirstAPI.Interfaces;
using FirstAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSheetApp.Models.DTOs;

namespace FirstAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IAuditLogService _auditLog;

        public EmployeeController(IEmployeeService employeeService, IAuditLogService auditLog)
        {
            _employeeService = employeeService;
            _auditLog        = auditLog;
        }

        private string GetUsername() => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpGet("profile")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<EmployeeProfileDto>> GetMyProfile()
        {
            var username = GetUsername();
            var result   = await _employeeService.GetEmployeeByUsername(username);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<EmployeeProfileDto>> GetById(int id)
        {
            var result = await _employeeService.GetEmployeeProfile(id);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<IEnumerable<EmployeeProfileDto>>> GetAll([FromQuery] GetAllEmployeesRequestDTO dto)
        {
            var result = await _employeeService.GetAllEmployees();
            return Ok(result);
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<EmployeeProfileDto>> UpdateMyProfile([FromBody] EmployeeUpdateDto dto)
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _employeeService.UpdateEmployee(employee.EmployeeId, dto);
            await _auditLog.LogAsync(username, "UPDATE", "Employee", employee.EmployeeId,
                "Updated own profile", GetIp());
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<EmployeeProfileDto>> UpdateById(int id, [FromBody] EmployeeUpdateDto dto)
        {
            var result = await _employeeService.UpdateEmployee(id, dto);
            await _auditLog.LogAsync(GetUsername(), "UPDATE", "Employee", id,
                $"Admin updated employee #{id}", GetIp());
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<EmployeeProfileDto>> Delete(int id)
        {
            var result = await _employeeService.DeleteEmployee(id);
            await _auditLog.LogAsync(GetUsername(), "DELETE", "Employee", id,
                $"Deleted employee #{id}", GetIp());
            return Ok(result);
        }
    }
}
