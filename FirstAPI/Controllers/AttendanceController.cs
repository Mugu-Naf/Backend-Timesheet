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

        public AttendanceController(IAttendanceService attendanceService, IEmployeeService employeeService)
        {
            _attendanceService = attendanceService;
            _employeeService = employeeService;
        }

        [HttpPost("check-in")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<AttendanceResponseDto>> CheckIn([FromBody] AttendanceCheckInDto dto)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _attendanceService.CheckIn(employee.EmployeeId, dto);
            return Ok(result);
        }

        [HttpPut("check-out")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<AttendanceResponseDto>> CheckOut([FromBody] AttendanceCheckOutDto dto)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _attendanceService.CheckOut(employee.EmployeeId, dto);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AttendanceResponseDto>> GetById(int id)
        {
            var result = await _attendanceService.GetAttendanceById(id);
            return Ok(result);
        }

        [HttpGet("my")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<IEnumerable<AttendanceResponseDto>>> GetMyAttendance()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _attendanceService.GetAttendanceByEmployee(employee.EmployeeId);
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
        public async Task<ActionResult<AttendanceReportDto>> GetReport(int employeeId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            var result = await _attendanceService.GetAttendanceReport(employeeId, fromDate, toDate);
            return Ok(result);
        }
    }
}
