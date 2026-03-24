using FirstAPI.Contexts;
using FirstAPI.Exceptions;
using FirstAPI.Interfaces;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FirstAPI.Services
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly IRepository<int, LeaveRequest> _leaveRequestRepository;
        private readonly TimeSheetContext _context;

        public LeaveRequestService(
            IRepository<int, LeaveRequest> leaveRequestRepository,
            TimeSheetContext context)
        {
            _leaveRequestRepository = leaveRequestRepository;
            _context = context;
        }

        private async Task<LeaveBalance> GetOrCreateBalance(int employeeId, int year)
        {
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId && lb.Year == year);
            if (balance == null)
            {
                balance = new LeaveBalance { EmployeeId = employeeId, Year = year };
                _context.LeaveBalances.Add(balance);
                await _context.SaveChangesAsync();
            }
            return balance;
        }

        public async Task<LeaveRequestResponseDto> CreateLeaveRequest(int employeeId, LeaveRequestCreateDto dto)
        {
            if (dto.StartDate.Date < DateTime.Today)
                throw new ValidationException("Start date cannot be in the past.");
            if (dto.EndDate < dto.StartDate)
                throw new ValidationException("End date must be after start date.");
            if (!Enum.TryParse<LeaveType>(dto.LeaveType, true, out var leaveType))
                throw new ValidationException($"Invalid leave type: {dto.LeaveType}.");

            var overlapping = await _leaveRequestRepository.GetQueryable()
                .AnyAsync(l => l.EmployeeId == employeeId
                    && l.Status != LeaveStatus.Rejected
                    && l.Status != LeaveStatus.Cancelled
                    && l.StartDate <= dto.EndDate
                    && l.EndDate >= dto.StartDate);
            if (overlapping)
                throw new DuplicateEntityException("An overlapping leave request already exists for this period.");

            if (leaveType != LeaveType.Unpaid)
            {
                var balance = await GetOrCreateBalance(employeeId, dto.StartDate.Year);
                int days = (int)(dto.EndDate.Date - dto.StartDate.Date).TotalDays + 1;
                int remaining = GetRemaining(balance, leaveType);
                if (remaining < days)
                    throw new ValidationException($"Insufficient {leaveType} leave balance. Remaining: {remaining} day(s), Requested: {days} day(s).");
            }

            var leaveRequest = new LeaveRequest
            {
                EmployeeId = employeeId,
                LeaveType  = leaveType,
                StartDate  = dto.StartDate.Date,
                EndDate    = dto.EndDate.Date,
                Reason     = dto.Reason,
                Status     = LeaveStatus.Pending,
                CreatedAt  = DateTime.UtcNow
            };
            await _leaveRequestRepository.Add(leaveRequest);
            return await MapToResponseDto(leaveRequest);
        }

        public async Task<LeaveRequestResponseDto> GetLeaveRequestById(int leaveRequestId)
        {
            var leaveRequest = await _leaveRequestRepository.GetQueryable()
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LeaveRequestId == leaveRequestId);
            if (leaveRequest == null)
                throw new EntityNotFoundException($"Leave request {leaveRequestId} not found.");
            return MapToDto(leaveRequest);
        }

        public async Task<IEnumerable<LeaveRequestResponseDto>> GetLeaveRequestsByEmployee(int employeeId)
        {
            var requests = await _leaveRequestRepository.GetQueryable()
                .Include(l => l.Employee)
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            return requests.Select(MapToDto);
        }

        public async Task<IEnumerable<LeaveRequestResponseDto>> GetAllLeaveRequests()
        {
            var requests = await _leaveRequestRepository.GetQueryable()
                .Include(l => l.Employee)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            return requests.Select(MapToDto);
        }

        public async Task<LeaveRequestResponseDto> ApproveLeaveRequest(int leaveRequestId, string reviewedBy)
        {
            var leaveRequest = await _leaveRequestRepository.Get(leaveRequestId);
            if (leaveRequest.Status != LeaveStatus.Pending)
                throw new ValidationException("Only pending leave requests can be approved.");

            if (leaveRequest.LeaveType != LeaveType.Unpaid)
            {
                var balance = await GetOrCreateBalance(leaveRequest.EmployeeId, leaveRequest.StartDate.Year);
                int days = (int)(leaveRequest.EndDate.Date - leaveRequest.StartDate.Date).TotalDays + 1;
                DeductBalance(balance, leaveRequest.LeaveType, days);
                await _context.SaveChangesAsync();
            }

            leaveRequest.Status     = LeaveStatus.Approved;
            leaveRequest.ReviewedBy = reviewedBy;
            leaveRequest.ReviewedAt = DateTime.UtcNow;
            await _leaveRequestRepository.Update(leaveRequest);
            return await MapToResponseDto(leaveRequest);
        }

        public async Task<LeaveRequestResponseDto> RejectLeaveRequest(int leaveRequestId, string reviewedBy)
        {
            var leaveRequest = await _leaveRequestRepository.Get(leaveRequestId);
            if (leaveRequest.Status != LeaveStatus.Pending)
                throw new ValidationException("Only pending leave requests can be rejected.");
            leaveRequest.Status     = LeaveStatus.Rejected;
            leaveRequest.ReviewedBy = reviewedBy;
            leaveRequest.ReviewedAt = DateTime.UtcNow;
            await _leaveRequestRepository.Update(leaveRequest);
            return await MapToResponseDto(leaveRequest);
        }

        public async Task<LeaveRequestResponseDto> CancelLeaveRequest(int leaveRequestId, int employeeId)
        {
            var leaveRequest = await _leaveRequestRepository.Get(leaveRequestId);
            if (leaveRequest.EmployeeId != employeeId)
                throw new UnAuthorizedException("You can only cancel your own leave requests.");
            if (leaveRequest.Status != LeaveStatus.Pending)
                throw new ValidationException("Only pending leave requests can be cancelled.");
            leaveRequest.Status = LeaveStatus.Cancelled;
            await _leaveRequestRepository.Update(leaveRequest);
            return await MapToResponseDto(leaveRequest);
        }

        public async Task<LeaveBalanceDto> GetLeaveBalance(int employeeId, int year)
        {
            var balance = await GetOrCreateBalance(employeeId, year);
            return new LeaveBalanceDto
            {
                Year           = balance.Year,
                CasualTotal    = balance.CasualTotal,
                CasualUsed     = balance.CasualUsed,
                SickTotal      = balance.SickTotal,
                SickUsed       = balance.SickUsed,
                EarnedTotal    = balance.EarnedTotal,
                EarnedUsed     = balance.EarnedUsed,
                MaternityTotal = balance.MaternityTotal,
                MaternityUsed  = balance.MaternityUsed,
                PaternityTotal = balance.PaternityTotal,
                PaternityUsed  = balance.PaternityUsed
            };
        }

        private int GetRemaining(LeaveBalance b, LeaveType type) => type switch
        {
            LeaveType.Casual    => b.CasualTotal    - b.CasualUsed,
            LeaveType.Sick      => b.SickTotal      - b.SickUsed,
            LeaveType.Earned    => b.EarnedTotal    - b.EarnedUsed,
            LeaveType.Maternity => b.MaternityTotal - b.MaternityUsed,
            LeaveType.Paternity => b.PaternityTotal - b.PaternityUsed,
            _                   => int.MaxValue
        };

        private void DeductBalance(LeaveBalance b, LeaveType type, int days)
        {
            switch (type)
            {
                case LeaveType.Casual:    b.CasualUsed    += days; break;
                case LeaveType.Sick:      b.SickUsed      += days; break;
                case LeaveType.Earned:    b.EarnedUsed    += days; break;
                case LeaveType.Maternity: b.MaternityUsed += days; break;
                case LeaveType.Paternity: b.PaternityUsed += days; break;
            }
        }

        private async Task<LeaveRequestResponseDto> MapToResponseDto(LeaveRequest leaveRequest)
        {
            var full = await _leaveRequestRepository.GetQueryable()
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LeaveRequestId == leaveRequest.LeaveRequestId);
            return MapToDto(full ?? leaveRequest);
        }

        private LeaveRequestResponseDto MapToDto(LeaveRequest l)
        {
            return new LeaveRequestResponseDto
            {
                LeaveRequestId = l.LeaveRequestId,
                EmployeeId     = l.EmployeeId,
                EmployeeName   = l.Employee != null ? $"{l.Employee.FirstName} {l.Employee.LastName}" : "",
                LeaveType      = l.LeaveType.ToString(),
                StartDate      = l.StartDate,
                EndDate        = l.EndDate,
                Reason         = l.Reason,
                Status         = l.Status.ToString(),
                ReviewedBy     = l.ReviewedBy,
                ReviewedAt     = l.ReviewedAt,
                CreatedAt      = l.CreatedAt
            };
        }
    }
}
