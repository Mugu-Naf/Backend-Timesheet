//using AutoMapper;
//using FirstAPI.Contexts;
//using FirstAPI.Exceptions;
//using FirstAPI.Interfaces;
//using FirstAPI.Models;
//using FirstAPI.Models.DTOs;
//using Microsoft.EntityFrameworkCore;

//namespace FirstAPI.Services
//{
//    public class LeaveRequestService : ILeaveRequestService
//    {
//        private readonly IRepository<int, LeaveRequest> _leaveRequestRepository;
//        //private readonly TimeSheetContext _context;
//        private readonly IMapper _mapper;

//        public LeaveRequestService(
//            IRepository<int, LeaveRequest> leaveRequestRepository,
//            TimeSheetContext context,
//            IMapper mapper)
//        {
//            _leaveRequestRepository = leaveRequestRepository;
//            //_context = context;
//            _mapper = mapper;
//        }

//        public async Task<LeaveRequestResponseDto> CreateLeaveRequest(int employeeId, LeaveRequestCreateDto dto)
//        {
//            if (dto.EndDate < dto.StartDate)
//                throw new Exceptions.ValidationException("End date must be after start date");

//            // Parse leave type
//            if (!Enum.TryParse<LeaveType>(dto.LeaveType, true, out var leaveType))
//                throw new Exceptions.ValidationException($"Invalid leave type: {dto.LeaveType}. Valid types: {string.Join(", ", Enum.GetNames<LeaveType>())}");

//            // Check for overlapping leaves
//            var overlapping = await _leaveRequestRepository.GetQueryable()
//                .AnyAsync(l => l.EmployeeId == employeeId
//                    && l.Status != LeaveStatus.Rejected
//                    && l.Status != LeaveStatus.Cancelled
//                    && l.StartDate <= dto.EndDate
//                    && l.EndDate >= dto.StartDate);

//            if (overlapping)
//                throw new DuplicateEntityException("An overlapping leave request already exists for this period");

//            var leaveRequest = new LeaveRequest
//            {
//                EmployeeId = employeeId,
//                LeaveType = leaveType,
//                StartDate = dto.StartDate.Date,
//                EndDate = dto.EndDate.Date,
//                Reason = dto.Reason,
//                Status = LeaveStatus.Pending,
//                CreatedAt = DateTime.UtcNow
//            };

//            await _leaveRequestRepository.Add(leaveRequest);
//            return await MapToResponseDto(leaveRequest);
//        }

//        public async Task<LeaveRequestResponseDto> GetLeaveRequestById(int leaveRequestId)
//        {
//            var leaveRequest = await _leaveRequestRepository.GetQueryable()
//                .Include(l => l.Employee)
//                .FirstOrDefaultAsync(l => l.LeaveRequestId == leaveRequestId);

//            if (leaveRequest == null)
//                throw new EntityNotFoundException($"Leave request with ID {leaveRequestId} not found");

//            return MapToDto(leaveRequest);
//        }

//        public async Task<IEnumerable<LeaveRequestResponseDto>> GetLeaveRequestsByEmployee(int employeeId)
//        {
//            var requests = await _leaveRequestRepository.GetQueryable()
//                .Include(l => l.Employee)
//                .Where(l => l.EmployeeId == employeeId)
//                .OrderByDescending(l => l.CreatedAt)
//                .ToListAsync();

//            return requests.Select(MapToDto);
//        }

//        public async Task<IEnumerable<LeaveRequestResponseDto>> GetAllLeaveRequests()
//        {
//            var requests = await _leaveRequestRepository.GetQueryable()
//                .Include(l => l.Employee)
//                .OrderByDescending(l => l.CreatedAt)
//                .ToListAsync();

//            return requests.Select(MapToDto);
//        }

//        public async Task<LeaveRequestResponseDto> ApproveLeaveRequest(int leaveRequestId, string reviewedBy)
//        {
//            var leaveRequest = await _leaveRequestRepository.Get(leaveRequestId);
//            if (leaveRequest.Status != LeaveStatus.Pending)
//                throw new Exceptions.ValidationException("Only pending leave requests can be approved");

//            leaveRequest.Status = LeaveStatus.Approved;
//            leaveRequest.ReviewedBy = reviewedBy;
//            leaveRequest.ReviewedAt = DateTime.UtcNow;

//            await _leaveRequestRepository.Update(leaveRequest);
//            return await MapToResponseDto(leaveRequest);
//        }

//        public async Task<LeaveRequestResponseDto> RejectLeaveRequest(int leaveRequestId, string reviewedBy)
//        {
//            var leaveRequest = await _leaveRequestRepository.Get(leaveRequestId);
//            if (leaveRequest.Status != LeaveStatus.Pending)
//                throw new Exceptions.ValidationException("Only pending leave requests can be rejected");

//            leaveRequest.Status = LeaveStatus.Rejected;
//            leaveRequest.ReviewedBy = reviewedBy;
//            leaveRequest.ReviewedAt = DateTime.UtcNow;

//            await _leaveRequestRepository.Update(leaveRequest);
//            return await MapToResponseDto(leaveRequest);
//        }

//        public async Task<LeaveRequestResponseDto> CancelLeaveRequest(int leaveRequestId, int employeeId)
//        {
//            var leaveRequest = await _leaveRequestRepository.Get(leaveRequestId);
//            if (leaveRequest.EmployeeId != employeeId)
//                throw new UnAuthorizedException("You can only cancel your own leave requests");
//            if (leaveRequest.Status != LeaveStatus.Pending)
//                throw new Exceptions.ValidationException("Only pending leave requests can be cancelled");

//            leaveRequest.Status = LeaveStatus.Cancelled;
//            await _leaveRequestRepository.Update(leaveRequest);
//            return await MapToResponseDto(leaveRequest);
//        }

//        private async Task<LeaveRequestResponseDto> MapToResponseDto(LeaveRequest leaveRequest)
//        {
//            var fullRequest = await _leaveRequestRepository.GetQueryable()
//                .Include(l => l.Employee)
//                .FirstOrDefaultAsync(l => l.LeaveRequestId == leaveRequest.LeaveRequestId);

//            return MapToDto(fullRequest ?? leaveRequest);
//        }

//        private LeaveRequestResponseDto MapToDto(LeaveRequest leaveRequest)
//        {
//            return new LeaveRequestResponseDto
//            {
//                LeaveRequestId = leaveRequest.LeaveRequestId,
//                EmployeeId = leaveRequest.EmployeeId,
//                EmployeeName = leaveRequest.Employee != null ? $"{leaveRequest.Employee.FirstName} {leaveRequest.Employee.LastName}" : "",
//                LeaveType = leaveRequest.LeaveType.ToString(),
//                StartDate = leaveRequest.StartDate,
//                EndDate = leaveRequest.EndDate,
//                Reason = leaveRequest.Reason,
//                Status = leaveRequest.Status.ToString(),
//                ReviewedBy = leaveRequest.ReviewedBy,
//                ReviewedAt = leaveRequest.ReviewedAt,
//                CreatedAt = leaveRequest.CreatedAt
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
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly IRepository<int, LeaveRequest> _leaveRequestRepository;
        private readonly IMapper _mapper;

        public LeaveRequestService(
            IRepository<int, LeaveRequest> leaveRequestRepository,
            IMapper mapper)
        {
            _leaveRequestRepository = leaveRequestRepository;
            _mapper = mapper;
        }

        public async Task<LeaveRequestResponseDto> CreateLeaveRequest(int employeeId, LeaveRequestCreateDto dto)
        {
            if (dto.EndDate < dto.StartDate)
                throw new Exceptions.ValidationException("End date must be after start date");

            // Parse leave type
            if (!Enum.TryParse<LeaveType>(dto.LeaveType, true, out var leaveType))
                throw new Exceptions.ValidationException($"Invalid leave type: {dto.LeaveType}. Valid types: {string.Join(", ", Enum.GetNames<LeaveType>())}");

            // Check for overlapping leaves
            var overlapping = await _leaveRequestRepository.GetQueryable()
                .AnyAsync(l => l.EmployeeId == employeeId
                    && l.Status != LeaveStatus.Rejected
                    && l.Status != LeaveStatus.Cancelled
                    && l.StartDate <= dto.EndDate
                    && l.EndDate >= dto.StartDate);

            if (overlapping)
                throw new DuplicateEntityException("An overlapping leave request already exists for this period");

            var leaveRequest = new LeaveRequest
            {
                EmployeeId = employeeId,
                LeaveType = leaveType,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                Reason = dto.Reason,
                Status = LeaveStatus.Pending,
                CreatedAt = DateTime.UtcNow
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
                throw new EntityNotFoundException($"Leave request with ID {leaveRequestId} not found");

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
                throw new Exceptions.ValidationException("Only pending leave requests can be approved");

            leaveRequest.Status = LeaveStatus.Approved;
            leaveRequest.ReviewedBy = reviewedBy;
            leaveRequest.ReviewedAt = DateTime.UtcNow;

            await _leaveRequestRepository.Update(leaveRequest);
            return await MapToResponseDto(leaveRequest);
        }

        public async Task<LeaveRequestResponseDto> RejectLeaveRequest(int leaveRequestId, string reviewedBy)
        {
            var leaveRequest = await _leaveRequestRepository.Get(leaveRequestId);
            if (leaveRequest.Status != LeaveStatus.Pending)
                throw new Exceptions.ValidationException("Only pending leave requests can be rejected");

            leaveRequest.Status = LeaveStatus.Rejected;
            leaveRequest.ReviewedBy = reviewedBy;
            leaveRequest.ReviewedAt = DateTime.UtcNow;

            await _leaveRequestRepository.Update(leaveRequest);
            return await MapToResponseDto(leaveRequest);
        }

        public async Task<LeaveRequestResponseDto> CancelLeaveRequest(int leaveRequestId, int employeeId)
        {
            var leaveRequest = await _leaveRequestRepository.Get(leaveRequestId);
            if (leaveRequest.EmployeeId != employeeId)
                throw new UnAuthorizedException("You can only cancel your own leave requests");
            if (leaveRequest.Status != LeaveStatus.Pending)
                throw new Exceptions.ValidationException("Only pending leave requests can be cancelled");

            leaveRequest.Status = LeaveStatus.Cancelled;
            await _leaveRequestRepository.Update(leaveRequest);
            return await MapToResponseDto(leaveRequest);
        }

        private async Task<LeaveRequestResponseDto> MapToResponseDto(LeaveRequest leaveRequest)
        {
            var fullRequest = await _leaveRequestRepository.GetQueryable()
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LeaveRequestId == leaveRequest.LeaveRequestId);

            return MapToDto(fullRequest ?? leaveRequest);
        }

        private LeaveRequestResponseDto MapToDto(LeaveRequest leaveRequest)
        {
            return new LeaveRequestResponseDto
            {
                LeaveRequestId = leaveRequest.LeaveRequestId,
                EmployeeId = leaveRequest.EmployeeId,
                EmployeeName = leaveRequest.Employee != null ? $"{leaveRequest.Employee.FirstName} {leaveRequest.Employee.LastName}" : "",
                LeaveType = leaveRequest.LeaveType.ToString(),
                StartDate = leaveRequest.StartDate,
                EndDate = leaveRequest.EndDate,
                Reason = leaveRequest.Reason,
                Status = leaveRequest.Status.ToString(),
                ReviewedBy = leaveRequest.ReviewedBy,
                ReviewedAt = leaveRequest.ReviewedAt,
                CreatedAt = leaveRequest.CreatedAt
            };
        }
    }
}