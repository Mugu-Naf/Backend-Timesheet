using FirstAPI.Models.DTOs;

namespace FirstAPI.Interfaces
{
    public interface ITimesheetService
    {
        Task<TimesheetResponseDto> CreateTimesheet(int employeeId, TimesheetCreateDto dto);
        Task<TimesheetResponseDto> UpdateTimesheet(int timesheetId, int employeeId, TimesheetUpdateDto dto);
        Task<TimesheetResponseDto> DeleteTimesheet(int timesheetId, int employeeId);
        Task<TimesheetResponseDto> GetTimesheetById(int timesheetId);
        Task<IEnumerable<TimesheetResponseDto>> GetTimesheetsByEmployee(int employeeId);
        Task<IEnumerable<TimesheetResponseDto>> GetAllTimesheets();
        Task<TimesheetResponseDto> ApproveTimesheet(int timesheetId, string reviewedBy);
        Task<TimesheetResponseDto> RejectTimesheet(int timesheetId, string reviewedBy);
    }
}
