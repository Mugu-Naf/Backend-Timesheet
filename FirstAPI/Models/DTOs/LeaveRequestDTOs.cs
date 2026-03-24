using System.ComponentModel.DataAnnotations;

namespace FirstAPI.Models.DTOs
{
    public class LeaveRequestCreateDto
    {
        [Required]
        public string LeaveType { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    public class LeaveBalanceDto
    {
        public int Year { get; set; }
        public int CasualTotal { get; set; }
        public int CasualUsed { get; set; }
        public int CasualRemaining => CasualTotal - CasualUsed;
        public int SickTotal { get; set; }
        public int SickUsed { get; set; }
        public int SickRemaining => SickTotal - SickUsed;
        public int EarnedTotal { get; set; }
        public int EarnedUsed { get; set; }
        public int EarnedRemaining => EarnedTotal - EarnedUsed;
        public int MaternityTotal { get; set; }
        public int MaternityUsed { get; set; }
        public int MaternityRemaining => MaternityTotal - MaternityUsed;
        public int PaternityTotal { get; set; }
        public int PaternityUsed { get; set; }
        public int PaternityRemaining => PaternityTotal - PaternityUsed;
    }

    public class LeaveRequestResponseDto
    {
        public int LeaveRequestId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
