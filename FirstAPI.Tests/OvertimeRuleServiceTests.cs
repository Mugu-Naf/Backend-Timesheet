using AutoMapper;
using FirstAPI.Contexts;
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
    public class OvertimeRuleServiceTests
    {
        private Mock<IRepository<int, OvertimeRule>> _ruleRepoMock;
        private TimeSheetContext _context;
        private IMapper _mapper;
        private OvertimeRuleService _service;

        [SetUp]
        public void Setup()
        {
            _ruleRepoMock = new Mock<IRepository<int, OvertimeRule>>();

            var options = new DbContextOptionsBuilder<TimeSheetContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TimeSheetContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _service = new OvertimeRuleService(_ruleRepoMock.Object, _context, _mapper);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task CreateRule_Valid_ReturnsOvertimeRuleResponse()
        {
            // Arrange
            var dto = new OvertimeRuleCreateDto
            {
                RuleName = "Standard OT",
                MaxRegularHours = 8,
                OvertimeMultiplier = 1.5m,
                EffectiveFrom = DateTime.UtcNow
            };

            _ruleRepoMock.Setup(r => r.Add(It.IsAny<OvertimeRule>()))
                .ReturnsAsync((OvertimeRule o) => { o.OvertimeRuleId = 1; return o; });

            // Act
            var result = await _service.CreateRule(dto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.RuleName, Is.EqualTo("Standard OT"));
            Assert.That(result.OvertimeMultiplier, Is.EqualTo(1.5m));
            Assert.That(result.IsActive, Is.True);
        }

        [Test]
        public async Task UpdateRule_Valid_ReturnsUpdated()
        {
            // Arrange
            var rule = new OvertimeRule { OvertimeRuleId = 1, RuleName = "Old", MaxRegularHours = 8, OvertimeMultiplier = 1.5m, IsActive = true };
            _ruleRepoMock.Setup(r => r.Get(1)).ReturnsAsync(rule);
            _ruleRepoMock.Setup(r => r.Update(It.IsAny<OvertimeRule>())).ReturnsAsync((OvertimeRule o) => o);

            var dto = new OvertimeRuleUpdateDto { RuleName = "Updated", MaxRegularHours = 6, OvertimeMultiplier = 2.0m, IsActive = true };

            // Act
            var result = await _service.UpdateRule(1, dto);

            // Assert
            Assert.That(result.RuleName, Is.EqualTo("Updated"));
            Assert.That(result.MaxRegularHours, Is.EqualTo(6));
            Assert.That(result.OvertimeMultiplier, Is.EqualTo(2.0m));
        }

        [Test]
        public async Task GetActiveRule_ActiveRuleExists_ReturnsRule()
        {
            // Arrange
            _context.OvertimeRules.Add(new OvertimeRule
            {
                OvertimeRuleId = 1,
                RuleName = "Current",
                MaxRegularHours = 8,
                OvertimeMultiplier = 1.5m,
                EffectiveFrom = DateTime.UtcNow.AddDays(-10),
                IsActive = true
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveRule();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.RuleName, Is.EqualTo("Current"));
        }

        [Test]
        public async Task GetActiveRule_NoActiveRule_ReturnsNull()
        {
            // Arrange
            _context.OvertimeRules.Add(new OvertimeRule
            {
                OvertimeRuleId = 1,
                RuleName = "Expired",
                MaxRegularHours = 8,
                OvertimeMultiplier = 1.5m,
                EffectiveFrom = DateTime.UtcNow.AddDays(-30),
                EffectiveTo = DateTime.UtcNow.AddDays(-10),
                IsActive = false
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveRule();

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetAllRules_ReturnsAllRules()
        {
            // Arrange
            var rules = new List<OvertimeRule>
            {
                new OvertimeRule { OvertimeRuleId = 1, RuleName = "R1" },
                new OvertimeRule { OvertimeRuleId = 2, RuleName = "R2" }
            };
            _ruleRepoMock.Setup(r => r.GetAll()).ReturnsAsync(rules);

            // Act
            var result = await _service.GetAllRules();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task DeleteRule_Valid_ReturnsDeleted()
        {
            // Arrange
            var rule = new OvertimeRule { OvertimeRuleId = 1, RuleName = "ToDelete" };
            _ruleRepoMock.Setup(r => r.Delete(1)).ReturnsAsync(rule);

            // Act
            var result = await _service.DeleteRule(1);

            // Assert
            Assert.That(result.RuleName, Is.EqualTo("ToDelete"));
        }
    }
}
