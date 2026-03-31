using FirstAPI.Models.DTOs;

namespace FirstAPI.Interfaces
{
    public interface IAttendanceService
    {
        Task<AttendanceResponseDto> CheckIn(int employeeId, AttendanceCheckInDto dto);
        Task<AttendanceResponseDto> CheckOut(int employeeId, AttendanceCheckOutDto dto);
        Task<AttendanceResponseDto> GetAttendanceById(int attendanceId);
        Task<IEnumerable<AttendanceResponseDto>> GetAttendanceByEmployee(int employeeId);
        Task<IEnumerable<AttendanceResponseDto>> GetAllAttendance();
        Task<AttendanceReportDto> GetAttendanceReport(int employeeId, DateTime fromDate, DateTime toDate);
        Task<AttendanceResponseDto> FixCheckout(int attendanceId, AttendanceCheckOutDto dto);
    }
}
