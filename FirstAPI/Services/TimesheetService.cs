using AutoMapper;
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
        private readonly IMapper _mapper;

        // IST = UTC+5:30
        private static DateTime NowIst() => DateTime.UtcNow.Add(TimeSpan.FromHours(5.5));
        private static bool IsWeekend(DateTime date) =>
            date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

        public TimesheetService(
            IRepository<int, Timesheet> timesheetRepository,
            IRepository<int, OvertimeRule> overtimeRuleRepository,
            IMapper mapper)
        {
            _timesheetRepository = timesheetRepository;
            _overtimeRuleRepository = overtimeRuleRepository;
            _mapper = mapper;
        }

        public async Task<TimesheetResponseDto> CreateTimesheet(int employeeId, TimesheetCreateDto dto)
        {
            var existing = await _timesheetRepository.GetQueryable()
                .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.Date.Date == dto.Date.Date);

            if (existing != null)
                throw new DuplicateEntityException($"Timesheet entry already exists for {dto.Date:yyyy-MM-dd}.");

            decimal overtimeHours;

            if (IsWeekend(dto.Date))
            {
                // Weekend: all hours are overtime at 2x
                overtimeHours = dto.HoursWorked;
            }
            else
            {
                var activeRule = await _overtimeRuleRepository.GetQueryable()
                    .FirstOrDefaultAsync(r => r.IsActive
                        && r.EffectiveFrom <= dto.Date
                        && (r.EffectiveTo == null || r.EffectiveTo >= dto.Date));

                decimal maxRegular = activeRule?.MaxRegularHours ?? 8.0m;
                overtimeHours = dto.HoursWorked > maxRegular ? dto.HoursWorked - maxRegular : 0;
            }

            var timesheet = new Timesheet
            {
                EmployeeId    = employeeId,
                Date          = dto.Date.Date,
                HoursWorked   = dto.HoursWorked,
                OvertimeHours = overtimeHours,
                ProjectId     = dto.ProjectId,
                Comments      = dto.Comments,
                Status        = TimesheetStatus.Pending,
                SubmittedAt   = NowIst()
            };

            await _timesheetRepository.Add(timesheet);
            return await MapToResponseDto(timesheet);
        }

        public async Task<TimesheetResponseDto> UpdateTimesheet(int timesheetId, int employeeId, TimesheetUpdateDto dto)
        {
            var timesheet = await _timesheetRepository.Get(timesheetId);
            if (timesheet.EmployeeId != employeeId)
                throw new UnAuthorizedException("You can only update your own timesheets.");
            if (timesheet.Status != TimesheetStatus.Pending)
                throw new Exceptions.ValidationException("Only pending timesheets can be updated.");

            decimal overtimeHours;

            if (IsWeekend(timesheet.Date))
            {
                overtimeHours = dto.HoursWorked;
            }
            else
            {
                var activeRule = await _overtimeRuleRepository.GetQueryable()
                    .FirstOrDefaultAsync(r => r.IsActive
                        && r.EffectiveFrom <= timesheet.Date
                        && (r.EffectiveTo == null || r.EffectiveTo >= timesheet.Date));

                decimal maxRegular = activeRule?.MaxRegularHours ?? 8.0m;
                overtimeHours = dto.HoursWorked > maxRegular ? dto.HoursWorked - maxRegular : 0;
            }

            timesheet.HoursWorked   = dto.HoursWorked;
            timesheet.OvertimeHours = overtimeHours;
            timesheet.ProjectId     = dto.ProjectId;
            timesheet.Comments      = dto.Comments;

            await _timesheetRepository.Update(timesheet);
            return await MapToResponseDto(timesheet);
        }

        public async Task<TimesheetResponseDto> DeleteTimesheet(int timesheetId, int employeeId)
        {
            var timesheet = await _timesheetRepository.Get(timesheetId);
            if (timesheet.EmployeeId != employeeId)
                throw new UnAuthorizedException("You can only delete your own timesheets.");
            if (timesheet.Status != TimesheetStatus.Pending)
                throw new Exceptions.ValidationException("Only pending timesheets can be deleted.");

            await _timesheetRepository.Delete(timesheetId);
            return await MapToResponseDto(timesheet);
        }

        public async Task<TimesheetResponseDto> GetTimesheetById(int timesheetId)
        {
            var timesheet = await _timesheetRepository.GetQueryable()
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId);

            if (timesheet == null)
                throw new EntityNotFoundException($"Timesheet {timesheetId} not found.");

            return MapToDto(timesheet);
        }

        public async Task<IEnumerable<TimesheetResponseDto>> GetTimesheetsByEmployee(int employeeId)
        {
            var timesheets = await _timesheetRepository.GetQueryable()
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .Where(t => t.EmployeeId == employeeId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return timesheets.Select(MapToDto);
        }

        public async Task<IEnumerable<TimesheetResponseDto>> GetAllTimesheets()
        {
            var timesheets = await _timesheetRepository.GetQueryable()
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
                throw new Exceptions.ValidationException("Only pending timesheets can be approved.");

            timesheet.Status     = TimesheetStatus.Approved;
            timesheet.ReviewedBy = reviewedBy;
            timesheet.ReviewedAt = NowIst();

            await _timesheetRepository.Update(timesheet);
            return await MapToResponseDto(timesheet);
        }

        public async Task<TimesheetResponseDto> RejectTimesheet(int timesheetId, string reviewedBy)
        {
            var timesheet = await _timesheetRepository.Get(timesheetId);
            if (timesheet.Status != TimesheetStatus.Pending)
                throw new Exceptions.ValidationException("Only pending timesheets can be rejected.");

            timesheet.Status     = TimesheetStatus.Rejected;
            timesheet.ReviewedBy = reviewedBy;
            timesheet.ReviewedAt = NowIst();

            await _timesheetRepository.Update(timesheet);
            return await MapToResponseDto(timesheet);
        }

        private async Task<TimesheetResponseDto> MapToResponseDto(Timesheet timesheet)
        {
            var full = await _timesheetRepository.GetQueryable()
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TimesheetId == timesheet.TimesheetId);

            return MapToDto(full ?? timesheet);
        }

        private TimesheetResponseDto MapToDto(Timesheet t)
        {
            return new TimesheetResponseDto
            {
                TimesheetId        = t.TimesheetId,
                EmployeeId         = t.EmployeeId,
                EmployeeName       = t.Employee != null ? $"{t.Employee.FirstName} {t.Employee.LastName}" : "",
                Date               = t.Date,
                HoursWorked        = t.HoursWorked,
                OvertimeHours      = t.OvertimeHours,
                OvertimeMultiplier = IsWeekend(t.Date) ? 2.0m : (t.OvertimeHours > 0 ? 1.5m : 1.0m),
                ProjectId          = t.ProjectId,
                ProjectName        = t.Project?.ProjectName,
                Status             = t.Status.ToString(),
                Comments           = t.Comments,
                SubmittedAt        = t.SubmittedAt,
                ReviewedBy         = t.ReviewedBy,
                ReviewedAt         = t.ReviewedAt,
                IsWeekend          = IsWeekend(t.Date)
            };
        }
    }
}
