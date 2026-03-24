using System.Security.Claims;
using FirstAPI.Interfaces;
using FirstAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirstAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TimesheetController : ControllerBase
    {
        private readonly ITimesheetService _timesheetService;
        private readonly IEmployeeService _employeeService;
        private readonly IAuditLogService _auditLog;

        public TimesheetController(ITimesheetService timesheetService,
                                   IEmployeeService employeeService,
                                   IAuditLogService auditLog)
        {
            _timesheetService = timesheetService;
            _employeeService  = employeeService;
            _auditLog         = auditLog;
        }

        private string GetUsername() => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpPost]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<TimesheetResponseDto>> Create([FromBody] TimesheetCreateDto dto)
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _timesheetService.CreateTimesheet(employee.EmployeeId, dto);
            await _auditLog.LogAsync(username, "CREATE", "Timesheet", result.TimesheetId,
                $"Submitted timesheet for {result.Date:yyyy-MM-dd} ({result.HoursWorked}h)", GetIp());
            return Ok(result);
        }

        // ✅ "my" BEFORE "{id:int}" to prevent route conflict
        [HttpGet("my")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<IEnumerable<TimesheetResponseDto>>> GetMyTimesheets()
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _timesheetService.GetTimesheetsByEmployee(employee.EmployeeId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TimesheetResponseDto>> GetById(int id)
        {
            var result = await _timesheetService.GetTimesheetById(id);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<IEnumerable<TimesheetResponseDto>>> GetAll()
        {
            var result = await _timesheetService.GetAllTimesheets();
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<TimesheetResponseDto>> Update(int id, [FromBody] TimesheetUpdateDto dto)
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _timesheetService.UpdateTimesheet(id, employee.EmployeeId, dto);
            await _auditLog.LogAsync(username, "UPDATE", "Timesheet", id,
                $"Updated timesheet #{id}", GetIp());
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<TimesheetResponseDto>> Delete(int id)
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _timesheetService.DeleteTimesheet(id, employee.EmployeeId);
            await _auditLog.LogAsync(username, "DELETE", "Timesheet", id,
                $"Deleted timesheet #{id}", GetIp());
            return Ok(result);
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<TimesheetResponseDto>> Approve(int id)
        {
            var username = GetUsername();
            var result   = await _timesheetService.ApproveTimesheet(id, username);
            await _auditLog.LogAsync(username, "APPROVE", "Timesheet", id,
                $"Approved timesheet #{id}", GetIp());
            return Ok(result);
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<TimesheetResponseDto>> Reject(int id)
        {
            var username = GetUsername();
            var result   = await _timesheetService.RejectTimesheet(id, username);
            await _auditLog.LogAsync(username, "REJECT", "Timesheet", id,
                $"Rejected timesheet #{id}", GetIp());
            return Ok(result);
        }
    }
}
