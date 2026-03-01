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

        public TimesheetController(ITimesheetService timesheetService, IEmployeeService employeeService)
        {
            _timesheetService = timesheetService;
            _employeeService = employeeService;
        }

        [HttpPost]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<TimesheetResponseDto>> Create([FromBody] TimesheetCreateDto dto)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _timesheetService.CreateTimesheet(employee.EmployeeId, dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<TimesheetResponseDto>> Update(int id, [FromBody] TimesheetUpdateDto dto)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _timesheetService.UpdateTimesheet(id, employee.EmployeeId, dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<TimesheetResponseDto>> Delete(int id)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _timesheetService.DeleteTimesheet(id, employee.EmployeeId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TimesheetResponseDto>> GetById(int id)
        {
            var result = await _timesheetService.GetTimesheetById(id);
            return Ok(result);
        }

        [HttpGet("my")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<IEnumerable<TimesheetResponseDto>>> GetMyTimesheets()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _timesheetService.GetTimesheetsByEmployee(employee.EmployeeId);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<IEnumerable<TimesheetResponseDto>>> GetAll()
        {
            var result = await _timesheetService.GetAllTimesheets();
            return Ok(result);
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<TimesheetResponseDto>> Approve(int id)
        {
            var reviewedBy = User.FindFirst(ClaimTypes.Name)?.Value!;
            var result = await _timesheetService.ApproveTimesheet(id, reviewedBy);
            return Ok(result);
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<TimesheetResponseDto>> Reject(int id)
        {
            var reviewedBy = User.FindFirst(ClaimTypes.Name)?.Value!;
            var result = await _timesheetService.RejectTimesheet(id, reviewedBy);
            return Ok(result);
        }
    }
}
