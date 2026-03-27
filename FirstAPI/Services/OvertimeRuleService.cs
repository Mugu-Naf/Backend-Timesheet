using AutoMapper;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FirstAPI.Services
{
    public class OvertimeRuleService : IOvertimeRuleService
    {
        private readonly IRepository<int, OvertimeRule> _overtimeRuleRepository;
        private readonly IMapper _mapper;

        public OvertimeRuleService(
            IRepository<int, OvertimeRule> overtimeRuleRepository,
            IMapper mapper)
        {
            _overtimeRuleRepository = overtimeRuleRepository;
            _mapper = mapper;
        }

        public async Task<OvertimeRuleResponseDto> CreateRule(OvertimeRuleCreateDto dto)
        {
            var rule = _mapper.Map<OvertimeRule>(dto);
            rule.IsActive = true;
            rule.CreatedAt = DateTime.UtcNow;

            await _overtimeRuleRepository.Add(rule);
            return _mapper.Map<OvertimeRuleResponseDto>(rule);
        }

        public async Task<OvertimeRuleResponseDto> UpdateRule(int ruleId, OvertimeRuleUpdateDto dto)
        {
            var rule = await _overtimeRuleRepository.Get(ruleId);

            rule.RuleName = dto.RuleName;
            rule.MaxRegularHours = dto.MaxRegularHours;
            rule.OvertimeMultiplier = dto.OvertimeMultiplier;
            rule.EffectiveFrom = dto.EffectiveFrom;
            rule.EffectiveTo = dto.EffectiveTo;
            rule.IsActive = dto.IsActive;

            await _overtimeRuleRepository.Update(rule);
            return _mapper.Map<OvertimeRuleResponseDto>(rule);
        }

        public async Task<OvertimeRuleResponseDto> DeleteRule(int ruleId)
        {
            var rule = await _overtimeRuleRepository.Delete(ruleId);
            return _mapper.Map<OvertimeRuleResponseDto>(rule);
        }

        public async Task<OvertimeRuleResponseDto> GetRuleById(int ruleId)
        {
            var rule = await _overtimeRuleRepository.Get(ruleId);
            return _mapper.Map<OvertimeRuleResponseDto>(rule);
        }

        public async Task<IEnumerable<OvertimeRuleResponseDto>> GetAllRules()
        {
            var rules = await _overtimeRuleRepository.GetAll();
            return _mapper.Map<IEnumerable<OvertimeRuleResponseDto>>(rules);
        }

        public async Task<OvertimeRuleResponseDto?> GetActiveRule()
        {
            var rule = await _overtimeRuleRepository.GetQueryable()
                .FirstOrDefaultAsync(r => r.IsActive && r.EffectiveFrom <= DateTime.UtcNow
                    && (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow));

            return rule != null ? _mapper.Map<OvertimeRuleResponseDto>(rule) : null;
        }
    }
}
