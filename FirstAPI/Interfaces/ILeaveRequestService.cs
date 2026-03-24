using FirstAPI.Models.DTOs;

namespace FirstAPI.Interfaces
{
    public interface ILeaveRequestService
    {
        Task<LeaveRequestResponseDto> CreateLeaveRequest(int employeeId, LeaveRequestCreateDto dto);
        Task<LeaveRequestResponseDto> GetLeaveRequestById(int leaveRequestId);
        Task<IEnumerable<LeaveRequestResponseDto>> GetLeaveRequestsByEmployee(int employeeId);
        Task<IEnumerable<LeaveRequestResponseDto>> GetAllLeaveRequests();
        Task<LeaveRequestResponseDto> ApproveLeaveRequest(int leaveRequestId, string reviewedBy);
        Task<LeaveRequestResponseDto> RejectLeaveRequest(int leaveRequestId, string reviewedBy);
        Task<LeaveRequestResponseDto> CancelLeaveRequest(int leaveRequestId, int employeeId);
        Task<LeaveBalanceDto> GetLeaveBalance(int employeeId, int year);
    }
}
