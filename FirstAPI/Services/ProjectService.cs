//using AutoMapper;
//using FirstAPI.Contexts;
//using FirstAPI.Exceptions;
//using FirstAPI.Interfaces;
//using FirstAPI.Models;
//using FirstAPI.Models.DTOs;
//using Microsoft.EntityFrameworkCore;

//namespace FirstAPI.Services
//{
//    public class ProjectService : IProjectService
//    {
//        private readonly IRepository<int, Project> _projectRepository;
//        private readonly TimeSheetContext _context;
//        private readonly IMapper _mapper;

//        public ProjectService(
//            IRepository<int, Project> projectRepository,
//            TimeSheetContext context,
//            IMapper mapper)
//        {
//            _projectRepository = projectRepository;
//            _context = context;
//            _mapper = mapper;
//        }

//        public async Task<ProjectResponseDto> CreateProject(ProjectCreateDto dto)
//        {
//            var existing = await _context.Projects
//                .FirstOrDefaultAsync(p => p.ProjectName == dto.ProjectName);

//            if (existing != null)
//                throw new DuplicateEntityException($"Project with name '{dto.ProjectName}' already exists");

//            var project = _mapper.Map<Project>(dto);
//            project.IsActive = true;
//            project.CreatedAt = DateTime.UtcNow;

//            await _projectRepository.Add(project);
//            return _mapper.Map<ProjectResponseDto>(project);
//        }

//        public async Task<ProjectResponseDto> UpdateProject(int projectId, ProjectUpdateDto dto)
//        {
//            var project = await _projectRepository.Get(projectId);

//            project.ProjectName = dto.ProjectName;
//            project.ClientName = dto.ClientName;
//            project.Description = dto.Description;
//            project.StartDate = dto.StartDate;
//            project.EndDate = dto.EndDate;
//            project.IsActive = dto.IsActive;

//            await _projectRepository.Update(project);
//            return _mapper.Map<ProjectResponseDto>(project);
//        }

//        public async Task<ProjectResponseDto> DeleteProject(int projectId)
//        {
//            var project = await _projectRepository.Delete(projectId);
//            return _mapper.Map<ProjectResponseDto>(project);
//        }

//        public async Task<ProjectResponseDto> GetProjectById(int projectId)
//        {
//            var project = await _projectRepository.Get(projectId);
//            return _mapper.Map<ProjectResponseDto>(project);
//        }

//        public async Task<IEnumerable<ProjectResponseDto>> GetAllProjects()
//        {
//            var projects = await _projectRepository.GetAll();
//            return _mapper.Map<IEnumerable<ProjectResponseDto>>(projects);
//        }

//        public async Task<IEnumerable<ProjectResponseDto>> GetActiveProjects()
//        {
//            var projects = await _context.Projects
//                .Where(p => p.IsActive)
//                .ToListAsync();

//            return _mapper.Map<IEnumerable<ProjectResponseDto>>(projects);
//        }
//    }
//}

using AutoMapper;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FirstAPI.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IRepository<int, Project> _projectRepository;
        private readonly IMapper _mapper;

        public ProjectService(
            IRepository<int, Project> projectRepository,
            IMapper mapper)
        {
            _projectRepository = projectRepository;
            _mapper = mapper;
        }

        public async Task<ProjectResponseDto> CreateProject(ProjectCreateDto dto)
        {
            var existing = await _projectRepository.GetQueryable()
                .FirstOrDefaultAsync(p => p.ProjectName == dto.ProjectName);

            if (existing != null)
                throw new DuplicateEntityException($"Project with name '{dto.ProjectName}' already exists");

            var project = _mapper.Map<Project>(dto);
            project.IsActive = true;
            project.CreatedAt = DateTime.UtcNow;

            await _projectRepository.Add(project);
            return _mapper.Map<ProjectResponseDto>(project);
        }

        public async Task<ProjectResponseDto> UpdateProject(int projectId, ProjectUpdateDto dto)
        {
            var project = await _projectRepository.Get(projectId);

            project.ProjectName = dto.ProjectName;
            project.ClientName = dto.ClientName;
            project.Description = dto.Description;
            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;
            project.IsActive = dto.IsActive;

            await _projectRepository.Update(project);
            return _mapper.Map<ProjectResponseDto>(project);
        }

        public async Task<ProjectResponseDto> DeleteProject(int projectId)
        {
            var project = await _projectRepository.Delete(projectId);
            return _mapper.Map<ProjectResponseDto>(project);
        }

        public async Task<ProjectResponseDto> GetProjectById(int projectId)
        {
            var project = await _projectRepository.Get(projectId);
            return _mapper.Map<ProjectResponseDto>(project);
        }

        public async Task<IEnumerable<ProjectResponseDto>> GetAllProjects()
        {
            var projects = await _projectRepository.GetAll();
            // Auto-deactivate projects whose end date has passed
            var now = DateTime.UtcNow.Date;
            bool anyChanged = false;
            foreach (var p in projects)
            {
                if (p.IsActive && p.EndDate.HasValue && p.EndDate.Value.Date < now)
                {
                    p.IsActive = false;
                    await _projectRepository.Update(p);
                    anyChanged = true;
                }
            }
            if (anyChanged)
                projects = await _projectRepository.GetAll();
            return _mapper.Map<IEnumerable<ProjectResponseDto>>(projects);
        }

        public async Task<IEnumerable<ProjectResponseDto>> GetActiveProjects()
        {
            var projects = await _projectRepository.GetQueryable()
                .Where(p => p.IsActive)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProjectResponseDto>>(projects);
        }
    }
}