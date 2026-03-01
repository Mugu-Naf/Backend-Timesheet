using FirstAPI.Models.DTOs;

namespace FirstAPI.Interfaces
{
    public interface IEmployeeService
    {
        Task<EmployeeProfileDto> GetEmployeeProfile(int employeeId);
        Task<EmployeeProfileDto> GetEmployeeByUsername(string username);
        Task<IEnumerable<EmployeeProfileDto>> GetAllEmployees();
        Task<EmployeeProfileDto> UpdateEmployee(int employeeId, EmployeeUpdateDto dto);
        Task<EmployeeProfileDto> DeleteEmployee(int employeeId);
    }
}
