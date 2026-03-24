namespace FirstAPI.Models.DTOs
{
    public class AuditLogResponseDto
    {
        public int AuditLogId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? Description { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = "Success";
    }

    public class AuditLogFilterDto
    {
        public string? Username { get; set; }
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AuditLogPagedResponseDto
    {
        public IEnumerable<AuditLogResponseDto> Items { get; set; } = new List<AuditLogResponseDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
