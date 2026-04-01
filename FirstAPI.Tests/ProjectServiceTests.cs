using AutoMapper;
using FirstAPI.Contexts;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Mappings;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using FirstAPI.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FirstAPI.Tests
{
    public class ProjectServiceTests : IDisposable
    {
        private readonly TimeSheetContext _context;
        private readonly Mock<IRepository<int, Project>> _projectRepoMock;
        private readonly IMapper _mapper;
        private readonly ProjectService _service;

        public ProjectServiceTests()
        {
            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new TimeSheetContext(options);

            _projectRepoMock = new Mock<IRepository<int, Project>>();

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _service = new ProjectService(_projectRepoMock.Object, _mapper);
        }

        public void Dispose() => _context.Dispose();

        // ── CreateProject Tests ──────────────────────────────────────

        [Fact]
        public async Task CreateProject_ValidData_ReturnsProjectDto()
        {
            // Arrange
            _projectRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Projects);

            _projectRepoMock.Setup(r => r.Add(It.IsAny<Project>()))
                .ReturnsAsync((Project p) => { p.ProjectId = 1; return p; });

            var dto = new ProjectCreateDto
            {
                ProjectName = "New Project",
                ClientName  = "Acme Corp",
                StartDate   = DateTime.Today,
                EndDate     = DateTime.Today.AddMonths(3)
            };

            // Act
            var result = await _service.CreateProject(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Project", result.ProjectName);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task CreateProject_DuplicateName_ThrowsDuplicateEntityException()
        {
            // Arrange — project with same name already exists
            _context.Projects.Add(new Project
            {
                ProjectId   = 1,
                ProjectName = "Existing Project",
                StartDate   = DateTime.Today,
                IsActive    = true
            });
            await _context.SaveChangesAsync();

            _projectRepoMock.Setup(r => r.GetQueryable())
                .Returns(_context.Projects);

            var dto = new ProjectCreateDto
            {
                ProjectName = "Existing Project",
                StartDate   = DateTime.Today
            };

            // Act & Assert
            await Assert.ThrowsAsync<DuplicateEntityException>(
                () => _service.CreateProject(dto));
        }

        // ── UpdateProject Tests ──────────────────────────────────────

        [Fact]
        public async Task UpdateProject_ValidData_UpdatesProject()
        {
            // Arrange
            var project = new Project
            {
                ProjectId   = 1,
                ProjectName = "Old Name",
                StartDate   = DateTime.Today,
                IsActive    = true
            };

            _projectRepoMock.Setup(r => r.Get(1)).ReturnsAsync(project);
            _projectRepoMock.Setup(r => r.Update(It.IsAny<Project>()))
                .ReturnsAsync((Project p) => p);

            var dto = new ProjectUpdateDto
            {
                ProjectName = "New Name",
                StartDate   = DateTime.Today,
                IsActive    = true
            };

            // Act
            var result = await _service.UpdateProject(1, dto);

            // Assert
            Assert.Equal("New Name", result.ProjectName);
        }

        // ── GetAllProjects Tests ─────────────────────────────────────

        [Fact]
        public async Task GetAllProjects_ExpiredProject_AutoDeactivates()
        {
            // Arrange — project with end date in the past
            var expiredProject = new Project
            {
                ProjectId   = 1,
                ProjectName = "Expired Project",
                StartDate   = DateTime.Today.AddMonths(-6),
                EndDate     = DateTime.Today.AddDays(-1), // expired yesterday
                IsActive    = true
            };

            _projectRepoMock.SetupSequence(r => r.GetAll())
                .ReturnsAsync(new List<Project> { expiredProject })
                .ReturnsAsync(new List<Project> { expiredProject });

            _projectRepoMock.Setup(r => r.Update(It.IsAny<Project>()))
                .ReturnsAsync((Project p) => p);

            // Act
            var result = await _service.GetAllProjects();

            // Assert — project should be deactivated
            _projectRepoMock.Verify(r => r.Update(It.Is<Project>(p => !p.IsActive)), Times.Once);
        }

        [Fact]
        public async Task GetAllProjects_ActiveProject_NotDeactivated()
        {
            // Arrange — project with future end date
            var activeProject = new Project
            {
                ProjectId   = 1,
                ProjectName = "Active Project",
                StartDate   = DateTime.Today,
                EndDate     = DateTime.Today.AddMonths(3), // future
                IsActive    = true
            };

            _projectRepoMock.Setup(r => r.GetAll())
                .ReturnsAsync(new List<Project> { activeProject });

            // Act
            var result = await _service.GetAllProjects();

            // Assert — Update should NOT be called for active project
            _projectRepoMock.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
        }

        // ── GetProjectById Tests ─────────────────────────────────────

        [Fact]
        public async Task GetProjectById_ValidId_ReturnsProject()
        {
            // Arrange
            var project = new Project
            {
                ProjectId   = 1,
                ProjectName = "Test Project",
                StartDate   = DateTime.Today,
                IsActive    = true
            };

            _projectRepoMock.Setup(r => r.Get(1)).ReturnsAsync(project);

            // Act
            var result = await _service.GetProjectById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Project", result.ProjectName);
        }
    }
}
