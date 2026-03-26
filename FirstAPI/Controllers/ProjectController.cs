using System.Security.Claims;
using FirstAPI.Contexts;
using FirstAPI.Interfaces;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirstAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly IAuditLogService _auditLog;
        private readonly TimeSheetContext _context;

        public ProjectController(IProjectService projectService, IAuditLogService auditLog, TimeSheetContext context)
        {
            _projectService = projectService;
            _auditLog       = auditLog;
            _context        = context;
        }

        private string GetUsername() => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpPost]
        public async Task<ActionResult<ProjectResponseDto>> Create([FromBody] ProjectCreateDto dto)
        {
            var result = await _projectService.CreateProject(dto);
            await _auditLog.LogAsync(GetUsername(), "CREATE", "Project", result.ProjectId,
                $"Created project '{result.ProjectName}'", GetIp());
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProjectResponseDto>> Update(int id, [FromBody] ProjectUpdateDto dto)
        {
            var result = await _projectService.UpdateProject(id, dto);
            await _auditLog.LogAsync(GetUsername(), "UPDATE", "Project", id,
                $"Updated project '{result.ProjectName}'", GetIp());
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ProjectResponseDto>> Delete(int id)
        {
            var result = await _projectService.DeleteProject(id);
            await _auditLog.LogAsync(GetUsername(), "DELETE", "Project", id,
                $"Deleted project '{result.ProjectName}'", GetIp());
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<ProjectResponseDto>> GetById(int id)
        {
            var result = await _projectService.GetProjectById(id);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetAll()
        {
            var result = await _projectService.GetAllProjects();
            return Ok(result);
        }

        [HttpGet("active")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetActive()
        {
            var result = await _projectService.GetActiveProjects();
            return Ok(result);
        }

        // ── Project Members ──────────────────────────────────────────
        [HttpGet("{id}/members")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult> GetMembers(int id)
        {
            var members = await _context.ProjectMembers
                .Include(pm => pm.Employee)
                .Where(pm => pm.ProjectId == id)
                .Select(pm => new {
                    pm.ProjectMemberId,
                    pm.EmployeeId,
                    EmployeeName = pm.Employee != null ? $"{pm.Employee.FirstName} {pm.Employee.LastName}" : "",
                    pm.Employee!.Department,
                    pm.Employee.JobTitle,
                    pm.AssignedAt
                })
                .ToListAsync();
            return Ok(members);
        }

        [HttpPost("{id}/members")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AddMember(int id, [FromBody] AddProjectMemberDto dto)
        {
            var exists = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == id && pm.EmployeeId == dto.EmployeeId);
            if (exists)
                return BadRequest(new { message = "Employee is already a member of this project." });

            var member = new ProjectMember { ProjectId = id, EmployeeId = dto.EmployeeId };
            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();
            await _auditLog.LogAsync(GetUsername(), "ADD_MEMBER", "Project", id,
                $"Added employee #{dto.EmployeeId} to project #{id}", GetIp());
            return Ok(new { message = "Employee added to project." });
        }

        [HttpDelete("{id}/members/{employeeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RemoveMember(int id, int employeeId)
        {
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == id && pm.EmployeeId == employeeId);
            if (member == null)
                return NotFound(new { message = "Member not found." });

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Employee removed from project." });
        }
    }
}

// DTO for adding member
namespace FirstAPI.Models.DTOs
{
    public class AddProjectMemberDto
    {
        public int EmployeeId { get; set; }
    }
}
