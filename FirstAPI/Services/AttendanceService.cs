//using AutoMapper;
//using FirstAPI.Contexts;
//using FirstAPI.Exceptions;
//using FirstAPI.Interfaces;
//using FirstAPI.Models;
//using FirstAPI.Models.DTOs;
//using Microsoft.EntityFrameworkCore;

//namespace FirstAPI.Services
//{
//    public class AttendanceService : IAttendanceService
//    {
//        private readonly IRepository<int, Attendance> _attendanceRepository;
//        private readonly IRepository<int, Employee> _employeeRepository;
//        //private readonly TimeSheetContext _context;
//        private readonly IMapper _mapper;

//        public AttendanceService(
//            IRepository<int, Attendance> attendanceRepository,
//            IRepository<int, Employee> employeeRepository,
//            TimeSheetContext context,
//            IMapper mapper)
//        {
//            _attendanceRepository = attendanceRepository;
//            _employeeRepository = employeeRepository;
//            _mapper = mapper;
//        }

//        public async Task<AttendanceResponseDto> CheckIn(int employeeId, AttendanceCheckInDto dto) 
//        {
//            var today = DateTime.UtcNow.Date;

//            // Check for duplicate check-in for today
//            var existing = await _attendanceRepository.GetQueryable()
//                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == today);

//            if (existing != null)
//                throw new DuplicateEntityException($"Attendance already recorded for employee {employeeId} on {today:yyyy-MM-dd}");

//            var attendance = new Attendance
//            {
//                EmployeeId = employeeId,
//                Date = today,
//                CheckInTime = dto.CheckInTime ?? DateTime.UtcNow,
//                Status = AttendanceStatus.Present
//            };

//            await _attendanceRepository.Add(attendance);
//            return await MapToResponseDto(attendance);
//        }

//        public async Task<AttendanceResponseDto> CheckOut(int employeeId, AttendanceCheckOutDto dto)
//        {
//            var today = DateTime.UtcNow.Date;

//            var attendance = await _attendanceRepository.GetQueryable()
//                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == today);

//            if (attendance == null)
//                throw new EntityNotFoundException("No check-in found for today. Please check in first.");

//            if (attendance.CheckOutTime != null)
//                throw new Exceptions.ValidationException("Already checked out for today");

//            attendance.CheckOutTime = dto.CheckOutTime ?? DateTime.UtcNow;

//            // Determine status based on hours worked
//            var hoursWorked = (attendance.CheckOutTime.Value - attendance.CheckInTime!.Value).TotalHours;
//            if (hoursWorked < 4)
//                attendance.Status = AttendanceStatus.HalfDay;

//            await _attendanceRepository.Update(attendance);
//            return await MapToResponseDto(attendance);
//        }

//        public async Task<AttendanceResponseDto> GetAttendanceById(int attendanceId)
//        {
//            var attendance = await _attendanceRepository.GetQueryable()
//                .Include(a => a.Employee)
//                .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

//            if (attendance == null)
//                throw new EntityNotFoundException($"Attendance record with ID {attendanceId} not found");

//            return MapToDto(attendance);
//        }

//        public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceByEmployee(int employeeId)
//        {
//            var records = await _attendanceRepository.GetQueryable()
//                .Include(a => a.Employee)
//                .Where(a => a.EmployeeId == employeeId)
//                .OrderByDescending(a => a.Date)
//                .ToListAsync();

//            return records.Select(MapToDto);
//        }

//        public async Task<IEnumerable<AttendanceResponseDto>> GetAllAttendance()
//        {
//            var records = await _attendanceRepository.GetQueryable()
//                .Include(a => a.Employee)
//                .OrderByDescending(a => a.Date)
//                .ToListAsync();

//            return records.Select(MapToDto);
//        }

//        public async Task<AttendanceReportDto> GetAttendanceReport(int employeeId, DateTime fromDate, DateTime toDate)
//        {
//            var employee = await _employeeRepository.Get(employeeId);
//            if (employee == null)
//                throw new EntityNotFoundException($"Employee with ID {employeeId} not found");

//            var records = await _attendanceRepository.GetQueryable()
//                .Where(a => a.EmployeeId == employeeId && a.Date >= fromDate && a.Date <= toDate)
//                .ToListAsync();

//            return new AttendanceReportDto
//            {
//                EmployeeId = employeeId,
//                EmployeeName = $"{employee.FirstName} {employee.LastName}",
//                TotalPresent = records.Count(r => r.Status == AttendanceStatus.Present),
//                TotalAbsent = records.Count(r => r.Status == AttendanceStatus.Absent),
//                TotalHalfDay = records.Count(r => r.Status == AttendanceStatus.HalfDay),
//                TotalLeave = records.Count(r => r.Status == AttendanceStatus.Leave),
//                FromDate = fromDate,
//                ToDate = toDate
//            };
//        }

//        private async Task<AttendanceResponseDto> MapToResponseDto(Attendance attendance)
//        {
//            var fullRecord = await _attendanceRepository.GetQueryable()
//                .Include(a => a.Employee)
//                .FirstOrDefaultAsync(a => a.AttendanceId == attendance.AttendanceId);

//            return MapToDto(fullRecord ?? attendance);
//        }

//        private AttendanceResponseDto MapToDto(Attendance attendance)
//        {
//            return new AttendanceResponseDto
//            {
//                AttendanceId = attendance.AttendanceId,
//                EmployeeId = attendance.EmployeeId,
//                EmployeeName = attendance.Employee != null ? $"{attendance.Employee.FirstName} {attendance.Employee.LastName}" : "",
//                Date = attendance.Date,
//                CheckInTime = attendance.CheckInTime,
//                CheckOutTime = attendance.CheckOutTime,
//                Status = attendance.Status.ToString()
//            };
//        }
//    }
//}


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
            var today = DateTime.UtcNow.Date;

            // Check for duplicate check-in for today
            var existing = await _attendanceRepository.GetQueryable()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == today);

            if (existing != null)
                throw new DuplicateEntityException($"Attendance already recorded for employee {employeeId} on {today:yyyy-MM-dd}");

            var rawCheckIn = dto.CheckInTime ?? DateTime.UtcNow;
            var attendance = new Attendance
            {
                EmployeeId = employeeId,
                Date = today,
                CheckInTime = new DateTime(rawCheckIn.Year, rawCheckIn.Month, rawCheckIn.Day, rawCheckIn.Hour, rawCheckIn.Minute, 0),
                Status = AttendanceStatus.Present
            };

            await _attendanceRepository.Add(attendance);
            return await MapToResponseDto(attendance);
        }

        public async Task<AttendanceResponseDto> CheckOut(int employeeId, AttendanceCheckOutDto dto)
        {
            var today = DateTime.UtcNow.Date;

            var attendance = await _attendanceRepository.GetQueryable()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == today);

            if (attendance == null)
                throw new EntityNotFoundException("No check-in found for today. Please check in first.");

            if (attendance.CheckOutTime != null)
                throw new Exceptions.ValidationException("Already checked out for today");

            var rawCheckOut = dto.CheckOutTime ?? DateTime.UtcNow;
            attendance.CheckOutTime = new DateTime(rawCheckOut.Year, rawCheckOut.Month, rawCheckOut.Day, rawCheckOut.Hour, rawCheckOut.Minute, 0);

            // Determine status based on hours worked
            var hoursWorked = (attendance.CheckOutTime.Value - attendance.CheckInTime!.Value).TotalHours;
            if (hoursWorked < 4)
                attendance.Status = AttendanceStatus.HalfDay;

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