using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirstAPI.Models
{
    public enum TimesheetStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Timesheet
    {
        [Key]
        public int TimesheetId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal HoursWorked { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal OvertimeHours { get; set; } = 0;

        public int? ProjectId { get; set; }

        [Required]
        public TimesheetStatus Status { get; set; } = TimesheetStatus.Pending;

        [MaxLength(500)]
        public string? Comments { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        // Navigation
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }
    }
}
