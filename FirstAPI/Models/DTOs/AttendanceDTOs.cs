using System.ComponentModel.DataAnnotations;

namespace FirstAPI.Models.DTOs
{
    public class AttendanceCheckInDto
    {
        public DateTime? CheckInTime { get; set; }
    }

    public class AttendanceCheckOutDto
    {
        public DateTime? CheckOutTime { get; set; }
    }

    public class AttendanceResponseDto
    {
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AttendanceReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalHalfDay { get; set; }
        public int TotalLeave { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}
