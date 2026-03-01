using AutoMapper;
using FirstAPI.Contexts;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FirstAPI.Services
{
    public class TimesheetService : ITimesheetService
    {
        private readonly IRepository<int, Timesheet> _timesheetRepository;
        private readonly IRepository<int, OvertimeRule> _overtimeRuleRepository;
        private readonly TimeSheetContext _context;
        private readonly IMapper _mapper;

        public TimesheetService(
            IRepository<int, Timesheet> timesheetRepository,
            IRepository<int, OvertimeRule> overtimeRuleRepository,
            TimeSheetContext context,
            IMapper mapper)
        {
            _timesheetRepository = timesheetRepository;
            _overtimeRuleRepository = overtimeRuleRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<TimesheetResponseDto> CreateTimesheet(int employeeId, TimesheetCreateDto dto)
        {
            // Check for duplicate entry (same employee + same date)
            var existingEntry = await _context.Timesheets
                .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.Date.Date == dto.Date.Date);

            if (existingEntry != null)
                throw new DuplicateEntityException($"Timesheet entry already exists for employee {employeeId} on {dto.Date:yyyy-MM-dd}");

            // Calculate overtime based on active rule
            decimal overtimeHours = 0;
            var activeRule = await _context.OvertimeRules
                .FirstOrDefaultAsync(r => r.IsActive && r.EffectiveFrom <= dto.Date && (r.EffectiveTo == null || r.EffectiveTo >= dto.Date));

            decimal maxRegularHours = activeRule?.MaxRegularHours ?? 8.0m;
            if (dto.HoursWorked > maxRegularHours)
                overtimeHours = dto.HoursWorked - maxRegularHours;

            var timesheet = new Timesheet
            {
                EmployeeId = employeeId,
                Date = dto.Date.Date,
                HoursWorked = dto.HoursWorked,
                OvertimeHours = overtimeHours,
                ProjectId = dto.ProjectId,
                Comments = dto.Comments,
                Status = TimesheetStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            await _timesheetRepository.Add(timesheet);
            return await MapToResponseDto(timesheet);
        }

        public async Task<TimesheetResponseDto> UpdateTimesheet(int timesheetId, int employeeId, TimesheetUpdateDto dto)
        {
            var timesheet = await _timesheetRepository.Get(timesheetId);
            if (timesheet.EmployeeId != employeeId)
                throw new UnAuthorizedException("You can only update your own timesheets");
            if (timesheet.Status != TimesheetStatus.Pending)
                throw new Exceptions.ValidationException("Only pending timesheets can be updated");

            // Recalculate overtime
            var activeRule = await _context.OvertimeRules
                .FirstOrDefaultAsync(r => r.IsActive && r.EffectiveFrom <= timesheet.Date && (r.EffectiveTo == null || r.EffectiveTo >= timesheet.Date));

            decimal maxRegularHours = activeRule?.MaxRegularHours ?? 8.0m;
            timesheet.HoursWorked = dto.HoursWorked;
            timesheet.OvertimeHours = dto.HoursWorked > maxRegularHours ? dto.HoursWorked - maxRegularHours : 0;
            timesheet.ProjectId = dto.ProjectId;
            timesheet.Comments = dto.Comments;

            await _timesheetRepository.Update(timesheet);
            return await MapToResponseDto(timesheet);
        }

        public async Task<TimesheetResponseDto> DeleteTimesheet(int timesheetId, int employeeId)
        {
            var timesheet = await _timesheetRepository.Get(timesheetId);
            if (timesheet.EmployeeId != employeeId)
                throw new UnAuthorizedException("You can only delete your own timesheets");
            if (timesheet.Status != TimesheetStatus.Pending)
                throw new Exceptions.ValidationException("Only pending timesheets can be deleted");

            await _timesheetRepository.Delete(timesheetId);
            return await MapToResponseDto(timesheet);
        }

        public async Task<TimesheetResponseDto> GetTimesheetById(int timesheetId)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId);

            if (timesheet == null)
                throw new EntityNotFoundException($"Timesheet with ID {timesheetId} not found");

            return MapToDto(timesheet);
        }

        public async Task<IEnumerable<TimesheetResponseDto>> GetTimesheetsByEmployee(int employeeId)
        {
            var timesheets = await _context.Timesheets
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .Where(t => t.EmployeeId == employeeId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return timesheets.Select(MapToDto);
        }

        public async Task<IEnumerable<TimesheetResponseDto>> GetAllTimesheets()
        {
            var timesheets = await _context.Timesheets
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return timesheets.Select(MapToDto);
        }

        public async Task<TimesheetResponseDto> ApproveTimesheet(int timesheetId, string reviewedBy)
        {
            var timesheet = await _timesheetRepository.Get(timesheetId);
            if (timesheet.Status != TimesheetStatus.Pending)
                throw new Exceptions.ValidationException("Only pending timesheets can be approved");

            timesheet.Status = TimesheetStatus.Approved;
            timesheet.ReviewedBy = reviewedBy;
            timesheet.ReviewedAt = DateTime.UtcNow;

            await _timesheetRepository.Update(timesheet);
            return await MapToResponseDto(timesheet);
        }

        public async Task<TimesheetResponseDto> RejectTimesheet(int timesheetId, string reviewedBy)
        {
            var timesheet = await _timesheetRepository.Get(timesheetId);
            if (timesheet.Status != TimesheetStatus.Pending)
                throw new Exceptions.ValidationException("Only pending timesheets can be rejected");

            timesheet.Status = TimesheetStatus.Rejected;
            timesheet.ReviewedBy = reviewedBy;
            timesheet.ReviewedAt = DateTime.UtcNow;

            await _timesheetRepository.Update(timesheet);
            return await MapToResponseDto(timesheet);
        }

        private async Task<TimesheetResponseDto> MapToResponseDto(Timesheet timesheet)
        {
            var fullTimesheet = await _context.Timesheets
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TimesheetId == timesheet.TimesheetId);

            return MapToDto(fullTimesheet ?? timesheet);
        }

        private TimesheetResponseDto MapToDto(Timesheet timesheet)
        {
            return new TimesheetResponseDto
            {
                TimesheetId = timesheet.TimesheetId,
                EmployeeId = timesheet.EmployeeId,
                EmployeeName = timesheet.Employee != null ? $"{timesheet.Employee.FirstName} {timesheet.Employee.LastName}" : "",
                Date = timesheet.Date,
                HoursWorked = timesheet.HoursWorked,
                OvertimeHours = timesheet.OvertimeHours,
                ProjectId = timesheet.ProjectId,
                ProjectName = timesheet.Project?.ProjectName,
                Status = timesheet.Status.ToString(),
                Comments = timesheet.Comments,
                SubmittedAt = timesheet.SubmittedAt,
                ReviewedBy = timesheet.ReviewedBy,
                ReviewedAt = timesheet.ReviewedAt
            };
        }
    }
}
