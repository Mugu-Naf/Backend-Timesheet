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
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet("profile")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<EmployeeProfileDto>> GetMyProfile()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var result = await _employeeService.GetEmployeeByUsername(username);
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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<EmployeeProfileDto>>> GetAll()
        {
            var result = await _employeeService.GetAllEmployees();
            return Ok(result);
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<EmployeeProfileDto>> UpdateMyProfile([FromBody] EmployeeUpdateDto dto)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value!;
            var employee = await _employeeService.GetEmployeeByUsername(username);
            var result = await _employeeService.UpdateEmployee(employee.EmployeeId, dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<EmployeeProfileDto>> UpdateById(int id, [FromBody] EmployeeUpdateDto dto)
        {
            var result = await _employeeService.UpdateEmployee(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<EmployeeProfileDto>> Delete(int id)
        {
            var result = await _employeeService.DeleteEmployee(id);
            return Ok(result);
        }
    }
}
