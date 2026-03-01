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
    [TestFixture]
    public class ProjectServiceTests
    {
        private Mock<IRepository<int, Project>> _projectRepoMock;
        private TimeSheetContext _context;
        private IMapper _mapper;
        private ProjectService _service;

        [SetUp]
        public void Setup()
        {
            _projectRepoMock = new Mock<IRepository<int, Project>>();

            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TimeSheetContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _service = new ProjectService(_projectRepoMock.Object, _context, _mapper);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task CreateProject_Valid_ReturnsProjectResponse()
        {
            // Arrange
            var dto = new ProjectCreateDto { ProjectName = "Project Alpha", ClientName = "Acme Corp", StartDate = DateTime.UtcNow };
            _projectRepoMock.Setup(r => r.Add(It.IsAny<Project>()))
                .ReturnsAsync((Project p) => { p.ProjectId = 1; return p; });

            // Act
            var result = await _service.CreateProject(dto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ProjectName, Is.EqualTo("Project Alpha"));
            Assert.That(result.IsActive, Is.True);
        }

        [Test]
        public async Task CreateProject_DuplicateName_ThrowsDuplicateEntityException()
        {
            // Arrange
            _context.Projects.Add(new Project { ProjectId = 1, ProjectName = "Duplicate" });
            await _context.SaveChangesAsync();

            var dto = new ProjectCreateDto { ProjectName = "Duplicate", StartDate = DateTime.UtcNow };

            // Act & Assert
            Assert.ThrowsAsync<DuplicateEntityException>(() => _service.CreateProject(dto));
        }

        [Test]
        public async Task GetAllProjects_ReturnsAllProjects()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { ProjectId = 1, ProjectName = "P1" },
                new Project { ProjectId = 2, ProjectName = "P2" }
            };
            _projectRepoMock.Setup(r => r.GetAll()).ReturnsAsync(projects);

            // Act
            var result = await _service.GetAllProjects();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetActiveProjects_ReturnsOnlyActive()
        {
            // Arrange
            _context.Projects.AddRange(
                new Project { ProjectId = 1, ProjectName = "Active1", IsActive = true },
                new Project { ProjectId = 2, ProjectName = "Inactive", IsActive = false },
                new Project { ProjectId = 3, ProjectName = "Active2", IsActive = true }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveProjects();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(p => p.IsActive), Is.True);
        }

        [Test]
        public async Task UpdateProject_Valid_ReturnsUpdated()
        {
            // Arrange
            var project = new Project { ProjectId = 1, ProjectName = "Old", IsActive = true };
            _projectRepoMock.Setup(r => r.Get(1)).ReturnsAsync(project);
            _projectRepoMock.Setup(r => r.Update(It.IsAny<Project>())).ReturnsAsync((Project p) => p);

            var dto = new ProjectUpdateDto { ProjectName = "New Name", IsActive = false, StartDate = DateTime.UtcNow };

            // Act
            var result = await _service.UpdateProject(1, dto);

            // Assert
            Assert.That(result.ProjectName, Is.EqualTo("New Name"));
            Assert.That(result.IsActive, Is.False);
        }

        [Test]
        public async Task DeleteProject_Valid_ReturnsDeleted()
        {
            // Arrange
            var project = new Project { ProjectId = 1, ProjectName = "ToDelete" };
            _projectRepoMock.Setup(r => r.Delete(1)).ReturnsAsync(project);

            // Act
            var result = await _service.DeleteProject(1);

            // Assert
            Assert.That(result.ProjectName, Is.EqualTo("ToDelete"));
        }
    }
}
