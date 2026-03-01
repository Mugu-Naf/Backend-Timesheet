using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirstAPI.Models
{
    public class OvertimeRule
    {
        [Key]
        public int OvertimeRuleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        /// <summary>
        /// Maximum regular working hours per day before overtime kicks in (e.g., 8.0).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal MaxRegularHours { get; set; } = 8.0m;

        /// <summary>
        /// Multiplier applied to overtime hours for pay calculation (e.g., 1.5 = time-and-a-half).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(4,2)")]
        public decimal OvertimeMultiplier { get; set; } = 1.5m;

        /// <summary>
        /// Date from which this overtime rule becomes effective.
        /// </summary>
        [Required]
        public DateTime EffectiveFrom { get; set; }

        /// <summary>
        /// Optional end date for this rule. Null means the rule is open-ended.
        /// </summary>
        public DateTime? EffectiveTo { get; set; }

        /// <summary>
        /// Whether this rule is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Timestamp when this rule was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
