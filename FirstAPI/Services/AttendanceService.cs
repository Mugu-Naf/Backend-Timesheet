using AutoMapper;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FirstAPI.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IRepository<int, Attendance> _attendanceRepository;
        private readonly IRepository<int, Employee> _employeeRepository;
        private readonly IMapper _mapper;

        public AttendanceService(
            IRepository<int, Attendance> attendanceRepository,
            IRepository<int, Employee> employeeRepository,
            IMapper mapper)
        {
            _attendanceRepository = attendanceRepository;
            _employeeRepository = employeeRepository;
            _mapper = mapper;
        }

        public async Task<AttendanceResponseDto> CheckIn(int employeeId, AttendanceCheckInDto dto)
        {
            var istOffset = TimeSpan.FromHours(5.5);
            var nowIst    = DateTime.UtcNow.Add(istOffset);
            var todayIst  = nowIst.Date;

            // Block if there's an open check-in from a previous day (forgot to check out)
            var openPrevious = await _attendanceRepository.GetQueryable()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                    && a.Date.Date < todayIst
                    && a.CheckInTime != null
                    && a.CheckOutTime == null);

            if (openPrevious != null)
                throw new Exceptions.ValidationException($"You forgot to check out on {openPrevious.Date:yyyy-MM-dd}. Please contact HR to fix it before checking in.");

            // Allow multiple sessions: only block if there's an OPEN (no checkout) session today
            var openToday = await _attendanceRepository.GetQueryable()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                    && a.Date.Date == todayIst
                    && a.CheckOutTime == null);

            if (openToday != null)
                throw new Exceptions.ValidationException("You are already checked in. Please check out first.");

            var checkIn = new DateTime(nowIst.Year, nowIst.Month, nowIst.Day, nowIst.Hour, nowIst.Minute, 0);
            var attendance = new Attendance
            {
                EmployeeId  = employeeId,
                Date        = todayIst,
                CheckInTime = checkIn,
                Status      = AttendanceStatus.Present
            };

            await _attendanceRepository.Add(attendance);
            return await MapToResponseDto(attendance);
        }

        public async Task<AttendanceResponseDto> CheckOut(int employeeId, AttendanceCheckOutDto dto)
        {
            var istOffset = TimeSpan.FromHours(5.5);
            var nowIst    = DateTime.UtcNow.Add(istOffset);
            var todayIst  = nowIst.Date;

            // Find the open session (no checkout yet) for today
            var attendance = await _attendanceRepository.GetQueryable()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                    && a.Date.Date == todayIst
                    && a.CheckOutTime == null);

            if (attendance == null)
                throw new EntityNotFoundException("No active check-in found for today. Please check in first.");

            var checkOut = new DateTime(nowIst.Year, nowIst.Month, nowIst.Day, nowIst.Hour, nowIst.Minute, 0);
            attendance.CheckOutTime = checkOut;

            // Calculate total hours across ALL completed sessions today
            var allTodaySessions = await _attendanceRepository.GetQueryable()
                .Where(a => a.EmployeeId == employeeId && a.Date.Date == todayIst && a.CheckOutTime != null)
                .ToListAsync();

            double totalHours = allTodaySessions
                .Sum(a => (a.CheckOutTime!.Value - a.CheckInTime!.Value).TotalHours);
            // Add current session
            totalHours += (checkOut - attendance.CheckInTime!.Value).TotalHours;

            // Status based on total hours for the day
            AttendanceStatus status;
            if (totalHours >= 4)
                status = AttendanceStatus.Present;
            else
                status = AttendanceStatus.HalfDay;

            attendance.Status = status;

            // Update all today's records to reflect the same status
            var allToday = await _attendanceRepository.GetQueryable()
                .Where(a => a.EmployeeId == employeeId && a.Date.Date == todayIst)
                .ToListAsync();
            foreach (var rec in allToday)
            {
                rec.Status = status;
                if (rec.AttendanceId != attendance.AttendanceId)
                    await _attendanceRepository.Update(rec);
            }

            await _attendanceRepository.Update(attendance);
            return await MapToResponseDto(attendance);
        }

        public async Task<AttendanceResponseDto> GetAttendanceById(int attendanceId)
        {
            var attendance = await _attendanceRepository.GetQueryable()
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

            if (attendance == null)
                throw new EntityNotFoundException($"Attendance record with ID {attendanceId} not found");

            return MapToDto(attendance);
        }

        public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceByEmployee(int employeeId)
        {
            var records = await _attendanceRepository.GetQueryable()
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            return records.Select(MapToDto);
        }

        public async Task<IEnumerable<AttendanceResponseDto>> GetAllAttendance()
        {
            var records = await _attendanceRepository.GetQueryable()
                .Include(a => a.Employee)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            return records.Select(MapToDto);
        }

        public async Task<AttendanceReportDto> GetAttendanceReport(int employeeId, DateTime fromDate, DateTime toDate)
        {
            var employee = await _employeeRepository.Get(employeeId);
            if (employee == null)
                throw new EntityNotFoundException($"Employee with ID {employeeId} not found");

            var records = await _attendanceRepository.GetQueryable()
                .Where(a => a.EmployeeId == employeeId && a.Date >= fromDate && a.Date <= toDate)
                .ToListAsync();

            return new AttendanceReportDto
            {
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                TotalPresent = records.Count(r => r.Status == AttendanceStatus.Present),
                TotalAbsent = records.Count(r => r.Status == AttendanceStatus.Absent),
                TotalHalfDay = records.Count(r => r.Status == AttendanceStatus.HalfDay),
                TotalLeave = records.Count(r => r.Status == AttendanceStatus.Leave),
                FromDate = fromDate,
                ToDate = toDate
            };
        }

        private async Task<AttendanceResponseDto> MapToResponseDto(Attendance attendance)
        {
            var fullRecord = await _attendanceRepository.GetQueryable()
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.AttendanceId == attendance.AttendanceId);

            return MapToDto(fullRecord ?? attendance);
        }

        private AttendanceResponseDto MapToDto(Attendance attendance)
        {
            return new AttendanceResponseDto
            {
                AttendanceId = attendance.AttendanceId,
                EmployeeId = attendance.EmployeeId,
                EmployeeName = attendance.Employee != null ? $"{attendance.Employee.FirstName} {attendance.Employee.LastName}" : "",
                Date = attendance.Date,
                CheckInTime = attendance.CheckInTime,
                CheckOutTime = attendance.CheckOutTime,
                Status = attendance.Status.ToString()
            };
        }
    }
}
