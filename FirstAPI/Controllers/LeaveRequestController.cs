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

        public LeaveRequestController(ILeaveRequestService leaveRequestService, IEmployeeService employeeService)
        {
            _leaveRequestService = leaveRequestService;
            _employeeService = employeeService;
        }

        [HttpPost]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<LeaveRequestResponseDto>> Create([FromBody] LeaveRequestCreateDto dto)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _leaveRequestService.CreateLeaveRequest(employee.EmployeeId, dto);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveRequestResponseDto>> GetById(int id)
        {
            var result = await _leaveRequestService.GetLeaveRequestById(id);
            return Ok(result);
        }

        [HttpGet("my")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<IEnumerable<LeaveRequestResponseDto>>> GetMyLeaveRequests()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _leaveRequestService.GetLeaveRequestsByEmployee(employee.EmployeeId);
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
            var reviewedBy = User.FindFirst(ClaimTypes.Name)?.Value!;
            var result = await _leaveRequestService.ApproveLeaveRequest(id, reviewedBy);
            return Ok(result);
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<LeaveRequestResponseDto>> Reject(int id)
        {
            var reviewedBy = User.FindFirst(ClaimTypes.Name)?.Value!;
            var result = await _leaveRequestService.RejectLeaveRequest(id, reviewedBy);
            return Ok(result);
        }

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<LeaveRequestResponseDto>> Cancel(int id)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _leaveRequestService.CancelLeaveRequest(id, employee.EmployeeId);
            return Ok(result);
        }
    }
}
