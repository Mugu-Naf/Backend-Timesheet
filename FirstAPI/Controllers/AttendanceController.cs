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
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IEmployeeService _employeeService;
        private readonly IAuditLogService _auditLog;

        public AttendanceController(IAttendanceService attendanceService,
                                    IEmployeeService employeeService,
                                    IAuditLogService auditLog)
        {
            _attendanceService = attendanceService;
            _employeeService   = employeeService;
            _auditLog          = auditLog;
        }

        private string GetUsername() => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpPost("check-in")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<AttendanceResponseDto>> CheckIn([FromBody] AttendanceCheckInDto dto)
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _attendanceService.CheckIn(employee.EmployeeId, dto);
            await _auditLog.LogAsync(username, "CHECK-IN", "Attendance", result.AttendanceId,
                $"Checked in at {result.CheckInTime:HH:mm}", GetIp());
            return Ok(result);
        }

        [HttpPut("check-out")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<AttendanceResponseDto>> CheckOut([FromBody] AttendanceCheckOutDto dto)
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _attendanceService.CheckOut(employee.EmployeeId, dto);
            await _auditLog.LogAsync(username, "CHECK-OUT", "Attendance", result.AttendanceId,
                $"Checked out at {result.CheckOutTime:HH:mm}", GetIp());
            return Ok(result);
        }

        // ✅ "my" BEFORE "{id:int}" to prevent route conflict
        [HttpGet("my")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<IEnumerable<AttendanceResponseDto>>> GetMyAttendance()
        {
            var username = GetUsername();
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result   = await _attendanceService.GetAttendanceByEmployee(employee.EmployeeId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AttendanceResponseDto>> GetById(int id)
        {
            var result = await _attendanceService.GetAttendanceById(id);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<IEnumerable<AttendanceResponseDto>>> GetAll()
        {
            var result = await _attendanceService.GetAllAttendance();
            return Ok(result);
        }

        [HttpGet("report/{employeeId}")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<AttendanceReportDto>> GetReport(int employeeId,
            [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            var result = await _attendanceService.GetAttendanceReport(employeeId, fromDate, toDate);
            return Ok(result);
        }

        /// <summary>
        /// Fix a forgotten checkout. HR/Admin can fix any. Employee can fix their own.
        /// </summary>
        [HttpPut("{attendanceId}/fix-checkout")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<AttendanceResponseDto>> FixCheckout(int attendanceId, [FromBody] AttendanceCheckOutDto dto)
        {
            var result = await _attendanceService.FixCheckout(attendanceId, dto);
            await _auditLog.LogAsync(GetUsername(), "FIX-CHECKOUT", "Attendance", attendanceId,
                $"Fixed checkout for attendance #{attendanceId}", GetIp());
            return Ok(result);
        }
    }
}
