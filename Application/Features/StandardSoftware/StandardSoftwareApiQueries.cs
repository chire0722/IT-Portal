// Project comment: StandardSoftwareApiQueries.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware
{
    /// <summary>
    /// Carries data needed to execute the GetStandardSoftwareKeys query.
    /// </summary>
    public sealed record GetStandardSoftwareKeysQuery(int SoftwareId) : IRequest<ProcessResult<StandardSoftwareKeysResponseDto>>;
    /// <summary>
    /// Carries data needed to execute the GetStandardSoftwareEmployees query.
    /// </summary>
    public sealed record GetStandardSoftwareEmployeesQuery() : IRequest<ProcessResult<List<StandardSoftwareEmployeeOptionDto>>>;

    /// <summary>
    /// Handles the GetStandardSoftwareKeysQuery query workflow.
    /// </summary>
    public sealed class GetStandardSoftwareKeysQueryHandler
        : IRequestHandler<GetStandardSoftwareKeysQuery, ProcessResult<StandardSoftwareKeysResponseDto>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILicenseEncryptionService _encryptionService;

        public GetStandardSoftwareKeysQueryHandler(ApplicationDbContext context, ILicenseEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<StandardSoftwareKeysResponseDto>> Handle(GetStandardSoftwareKeysQuery request, CancellationToken cancellationToken)
        {
            var license = await _context.SoftwareLicenses
                .AsNoTracking()
                .Include(l => l.Keys)
                    .ThenInclude(k => k.OnboardingRequest)
                .FirstOrDefaultAsync(l => l.Id == request.SoftwareId, cancellationToken);

            if (license == null)
            {
                return ProcessResult<StandardSoftwareKeysResponseDto>.NotFound();
            }

            var keys = license.Keys.OrderByDescending(k => k.CreatedAt).ToList();
            var result = new StandardSoftwareKeysResponseDto
            {
                IsSubscription = license.IsSubscription,
                ContractLimit = license.ContractLimit,
                Keys = keys.Select(k => new StandardSoftwareKeyDto
                {
                    Id = k.Id,
                    KeyValue = license.IsSubscription
                        ? (k.IsAssigned ? _encryptionService.DecryptKey(k.KeyValue ?? string.Empty) : "Subscription Seat")
                        : _encryptionService.DecryptKey(k.KeyValue ?? string.Empty),
                    IsAssigned = k.IsAssigned,
                    AssignedTo = k.OnboardingRequest != null ? k.OnboardingRequest.FullName : null,
                    OnboardingRequestId = k.OnboardingRequestId,
                    ExpiryDate = license.ExpiryDate
                }).ToList()
            };

            return ProcessResult<StandardSoftwareKeysResponseDto>.Success(result);
        }
    }

    /// <summary>
    /// Handles the GetStandardSoftwareEmployeesQuery query workflow.
    /// </summary>
    public sealed class GetStandardSoftwareEmployeesQueryHandler
        : IRequestHandler<GetStandardSoftwareEmployeesQuery, ProcessResult<List<StandardSoftwareEmployeeOptionDto>>>
    {
        private readonly ApplicationDbContext _context;

        public GetStandardSoftwareEmployeesQueryHandler(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<List<StandardSoftwareEmployeeOptionDto>>> Handle(GetStandardSoftwareEmployeesQuery request, CancellationToken cancellationToken)
        {
            var employees = await _context.OnboardingRequests
                .AsNoTracking()
                .Where(o => o.Status == RequestStatus.Approved || o.Status == RequestStatus.Completed)
                .OrderBy(o => o.FullName)
                .ProjectToType<StandardSoftwareEmployeeOptionDto>()
                .ToListAsync(cancellationToken);

            return ProcessResult<List<StandardSoftwareEmployeeOptionDto>>.Success(employees);
        }
    }
}
