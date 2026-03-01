using FirstAPI.Interfaces;
using FirstAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirstAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "HR,Admin")]
    public class OvertimeRuleController : ControllerBase
    {
        private readonly IOvertimeRuleService _overtimeRuleService;

        public OvertimeRuleController(IOvertimeRuleService overtimeRuleService)
        {
            _overtimeRuleService = overtimeRuleService;
        }

        [HttpPost]
        public async Task<ActionResult<OvertimeRuleResponseDto>> Create([FromBody] OvertimeRuleCreateDto dto)
        {
            var result = await _overtimeRuleService.CreateRule(dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<OvertimeRuleResponseDto>> Update(int id, [FromBody] OvertimeRuleUpdateDto dto)
        {
            var result = await _overtimeRuleService.UpdateRule(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<OvertimeRuleResponseDto>> Delete(int id)
        {
            var result = await _overtimeRuleService.DeleteRule(id);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OvertimeRuleResponseDto>> GetById(int id)
        {
            var result = await _overtimeRuleService.GetRuleById(id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OvertimeRuleResponseDto>>> GetAll()
        {
            var result = await _overtimeRuleService.GetAllRules();
            return Ok(result);
        }

        [HttpGet("active")]
        [Authorize(Roles = "Employee,HR,Admin")]
        public async Task<ActionResult<OvertimeRuleResponseDto>> GetActive()
        {
            var result = await _overtimeRuleService.GetActiveRule();
            if (result == null)
                throw new Exceptions.EntityNotFoundException("No active overtime rule found");
            return Ok(result);
        }
    }
}
