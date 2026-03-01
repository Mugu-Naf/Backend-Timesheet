using FirstAPI.Models.DTOs;

namespace FirstAPI.Interfaces
{
    public interface IOvertimeRuleService
    {
        Task<OvertimeRuleResponseDto> CreateRule(OvertimeRuleCreateDto dto);
        Task<OvertimeRuleResponseDto> UpdateRule(int ruleId, OvertimeRuleUpdateDto dto);
        Task<OvertimeRuleResponseDto> DeleteRule(int ruleId);
        Task<OvertimeRuleResponseDto> GetRuleById(int ruleId);
        Task<IEnumerable<OvertimeRuleResponseDto>> GetAllRules();
        Task<OvertimeRuleResponseDto?> GetActiveRule();
    }
}
