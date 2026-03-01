using FirstAPI.Interfaces;
using FirstAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirstAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "HR,Admin")]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpPost]
        public async Task<ActionResult<ProjectResponseDto>> Create([FromBody] ProjectCreateDto dto)
        {
            var result = await _projectService.CreateProject(dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProjectResponseDto>> Update(int id, [FromBody] ProjectUpdateDto dto)
        {
            var result = await _projectService.UpdateProject(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ProjectResponseDto>> Delete(int id)
        {
            var result = await _projectService.DeleteProject(id);
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
    }
}
