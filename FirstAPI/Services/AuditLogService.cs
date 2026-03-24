using FirstAPI.Contexts;
using FirstAPI.Interfaces;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FirstAPI.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly TimeSheetContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(TimeSheetContext context, ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(string username, string action, string entityType,
                                   int? entityId = null, string? description = null,
                                   string? ipAddress = null, string status = "Success")
        {
            try
            {
                var log = new AuditLog
                {
                    Username    = username,
                    Action      = action,
                    EntityType  = entityType,
                    EntityId    = entityId,
                    Description = description,
                    IpAddress   = ipAddress,
                    Status      = status,
                    Timestamp   = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Audit logging must never break the main flow
                _logger.LogError(ex, "Failed to write audit log for {Username} / {Action} / {EntityType}", username, action, entityType);
            }
        }

        public async Task<AuditLogPagedResponseDto> GetLogsAsync(AuditLogFilterDto filter)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Username))
                query = query.Where(l => l.Username.Contains(filter.Username));

            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(l => l.Action == filter.Action);

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                query = query.Where(l => l.EntityType == filter.EntityType);

            if (filter.FromDate.HasValue)
                query = query.Where(l => l.Timestamp >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(l => l.Timestamp <= filter.ToDate.Value.AddDays(1));

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            var items = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(l => new AuditLogResponseDto
                {
                    AuditLogId  = l.AuditLogId,
                    Username    = l.Username,
                    Action      = l.Action,
                    EntityType  = l.EntityType,
                    EntityId    = l.EntityId,
                    Description = l.Description,
                    IpAddress   = l.IpAddress,
                    Timestamp   = l.Timestamp,
                    Status      = l.Status
                })
                .ToListAsync();

            return new AuditLogPagedResponseDto
            {
                Items      = items,
                TotalCount = totalCount,
                Page       = filter.Page,
                PageSize   = filter.PageSize,
                TotalPages = totalPages
            };
        }

        public async Task<IEnumerable<AuditLogResponseDto>> GetRecentLogsAsync(int count = 10)
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .Select(l => new AuditLogResponseDto
                {
                    AuditLogId  = l.AuditLogId,
                    Username    = l.Username,
                    Action      = l.Action,
                    EntityType  = l.EntityType,
                    EntityId    = l.EntityId,
                    Description = l.Description,
                    IpAddress   = l.IpAddress,
                    Timestamp   = l.Timestamp,
                    Status      = l.Status
                })
                .ToListAsync();
        }
    }
}
