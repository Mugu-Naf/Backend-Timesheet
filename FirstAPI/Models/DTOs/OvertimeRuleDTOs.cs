using System.ComponentModel.DataAnnotations;

namespace FirstAPI.Models.DTOs
{
    public class OvertimeRuleCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [Required]
        [Range(0.5, 24)]
        public decimal MaxRegularHours { get; set; } = 8.0m;

        [Required]
        [Range(1.0, 5.0)]
        public decimal OvertimeMultiplier { get; set; } = 1.5m;

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }
    }

    public class OvertimeRuleUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [Required]
        [Range(0.5, 24)]
        public decimal MaxRegularHours { get; set; }

        [Required]
        [Range(1.0, 5.0)]
        public decimal OvertimeMultiplier { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }
    }

    public class OvertimeRuleResponseDto
    {
        public int OvertimeRuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public decimal MaxRegularHours { get; set; }
        public decimal OvertimeMultiplier { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
