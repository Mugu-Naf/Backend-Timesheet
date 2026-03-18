using FirstAPI.Models.DTOs;

namespace FirstAPI.Interfaces
{
    public interface IEmployeeService
    {
        Task<EmployeeProfileDto> GetEmployeeByUsername(string username);
        Task<EmployeeProfileDto> GetEmployeeProfile(int id);
        Task<IEnumerable<EmployeeProfileDto>> GetAllEmployees();
        Task<EmployeeProfileDto> UpdateEmployee(int id, EmployeeUpdateDto dto);
        Task<EmployeeProfileDto> DeleteEmployee(int id);
        Task<EmployeeProfileDto> CreateEmployee(EmployeeCreateDto dto);
    }
}
