using AutoMapper;
using FirstAPI.Contexts;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FirstAPI.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IRepository<int, Employee> _employeeRepository;
        private readonly TimeSheetContext _context;
        private readonly IMapper _mapper;

        public EmployeeService(
            IRepository<int, Employee> employeeRepository,
            TimeSheetContext context,
            IMapper mapper)
        {
            _employeeRepository = employeeRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<EmployeeProfileDto> GetEmployeeProfile(int employeeId)
        {
            var employee = await _employeeRepository.Get(employeeId);
            return _mapper.Map<EmployeeProfileDto>(employee);
        }

        public async Task<EmployeeProfileDto> GetEmployeeByUsername(string username)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Username == username);

            if (employee == null)
                throw new EntityNotFoundException($"Employee with username '{username}' not found");

            return _mapper.Map<EmployeeProfileDto>(employee);
        }

        public async Task<IEnumerable<EmployeeProfileDto>> GetAllEmployees()
        {
            var employees = await _employeeRepository.GetAll();
            return _mapper.Map<IEnumerable<EmployeeProfileDto>>(employees);
        }

        public async Task<EmployeeProfileDto> UpdateEmployee(int employeeId, EmployeeUpdateDto dto)
        {
            var employee = await _employeeRepository.Get(employeeId);

            employee.FirstName = dto.FirstName;
            employee.LastName = dto.LastName;
            employee.Email = dto.Email;
            employee.Phone = dto.Phone;
            employee.Department = dto.Department;
            employee.JobTitle = dto.JobTitle;

            await _employeeRepository.Update(employee);
            return _mapper.Map<EmployeeProfileDto>(employee);
        }

        public async Task<EmployeeProfileDto> DeleteEmployee(int employeeId)
        {
            var employee = await _employeeRepository.Delete(employeeId);
            return _mapper.Map<EmployeeProfileDto>(employee);
        }
    }
}
