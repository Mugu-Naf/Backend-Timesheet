using FirstAPI.Models.DTOs;

namespace FirstAPI.Interfaces
{
    public interface IProjectService
    {
        Task<ProjectResponseDto> CreateProject(ProjectCreateDto dto);
        Task<ProjectResponseDto> UpdateProject(int projectId, ProjectUpdateDto dto);
        Task<ProjectResponseDto> DeleteProject(int projectId);
        Task<ProjectResponseDto> GetProjectById(int projectId);
        Task<IEnumerable<ProjectResponseDto>> GetAllProjects();
        Task<IEnumerable<ProjectResponseDto>> GetActiveProjects();
    }
}
