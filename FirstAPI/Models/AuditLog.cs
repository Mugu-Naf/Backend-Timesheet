using System.ComponentModel.DataAnnotations;

namespace FirstAPI.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty;   // CREATE, UPDATE, DELETE, APPROVE, REJECT, LOGIN, LOGOUT

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty; // Timesheet, LeaveRequest, Project, etc.

        public int? EntityId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "Success"; // Success / Failed
    }
}
