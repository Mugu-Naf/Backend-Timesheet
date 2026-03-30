using System.ComponentModel.DataAnnotations;

namespace FirstAPI.Models.DTOs
{
    public class TimesheetCreateDto
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Range(0.25, 24)]
        public decimal HoursWorked { get; set; }

        public int? ProjectId { get; set; }

        [MaxLength(500)]
        public string? Comments { get; set; }
    }

    public class TimesheetUpdateDto
    {
        [Required]
        [Range(0.25, 24)]
        public decimal HoursWorked { get; set; }

        public int? ProjectId { get; set; }

        [MaxLength(500)]
        public string? Comments { get; set; }
    }

    public class TimesheetResponseDto
    {
        public int TimesheetId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal OvertimeHours { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public bool IsWeekend { get; set; }
        // Effective overtime multiplier: 2.0 on weekends, from OvertimeRule on weekdays
        public decimal OvertimeMultiplier { get; set; } = 1.0m;
    }

    public class TimesheetApprovalDto
    {
        [MaxLength(500)]
        public string? Comments { get; set; }
    }
}
