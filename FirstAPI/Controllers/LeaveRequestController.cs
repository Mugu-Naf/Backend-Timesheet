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
    public class LeaveRequestController : ControllerBase
    {
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly IEmployeeService _employeeService;
        private readonly IAuditLogService _auditLog;

        public LeaveRequestController(ILeaveRequestService leaveRequestService,
                                      IEmployeeService employeeService,
                                      IAuditLogService auditLog)
        {
            _leaveRequestService = leaveRequestService;
            _employeeService     = employeeService;
            _auditLog            = auditLog;
        }

        private string GetUsername() => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpPost]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<LeaveRequestResponseDto>> Create([FromBody] LeaveRequestCreateDto dto)
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _leaveRequestService.CreateLeaveRequest(employee.EmployeeId, dto);
            await _auditLog.LogAsync(username, "CREATE", "LeaveRequest", result.LeaveRequestId,
                $"Applied {result.LeaveType} leave from {result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd}", GetIp());
            return Ok(result);
        }

        // ✅ "my" MUST come before "{id}" to avoid route conflict
        [HttpGet("my")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<IEnumerable<LeaveRequestResponseDto>>> GetMyLeaveRequests()
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _leaveRequestService.GetLeaveRequestsByEmployee(employee.EmployeeId);
            return Ok(result);
        }

        [HttpGet("balance")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<LeaveBalanceDto>> GetMyBalance()
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _leaveRequestService.GetLeaveBalance(employee.EmployeeId, DateTime.Today.Year);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<LeaveRequestResponseDto>> GetById(int id)
        {
            var result = await _leaveRequestService.GetLeaveRequestById(id);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<IEnumerable<LeaveRequestResponseDto>>> GetAll()
        {
            var result = await _leaveRequestService.GetAllLeaveRequests();
            return Ok(result);
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<LeaveRequestResponseDto>> Approve(int id)
        {
            var username = GetUsername();
            var result   = await _leaveRequestService.ApproveLeaveRequest(id, username);
            await _auditLog.LogAsync(username, "APPROVE", "LeaveRequest", id,
                $"Approved leave request #{id}", GetIp());
            return Ok(result);
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<LeaveRequestResponseDto>> Reject(int id)
        {
            var username = GetUsername();
            var result   = await _leaveRequestService.RejectLeaveRequest(id, username);
            await _auditLog.LogAsync(username, "REJECT", "LeaveRequest", id,
                $"Rejected leave request #{id}", GetIp());
            return Ok(result);
        }

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<LeaveRequestResponseDto>> Cancel(int id)
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _leaveRequestService.CancelLeaveRequest(id, employee.EmployeeId);
            await _auditLog.LogAsync(username, "CANCEL", "LeaveRequest", id,
                $"Cancelled leave request #{id}", GetIp());
            return Ok(result);
        }
    }
}
