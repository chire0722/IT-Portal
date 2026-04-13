using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Services;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = IT_Form.Models.Company;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDetails
{
    public sealed class GetOnboardingDetailsQueryHandler : IRequestHandler<GetOnboardingDetailsQuery, GetOnboardingDetailsResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetOnboardingDetailsQueryHandler(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISignatureIntegrityService integrityService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _userManager = userManager;
            _integrityService = integrityService;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<GetOnboardingDetailsResult> Handle(GetOnboardingDetailsQuery request, CancellationToken cancellationToken)
        {
            var onboardingRequest = await _context.OnboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .Include(r => r.IssuedAssets)
                .Include(r => r.IssuedSoftware)
                .Include(r => r.FolderPermissions)
                .Include(r => r.RequestSoftware)
                .Include(r => r.RequestEmailGroups)
                .Include(r => r.RequestSharedMailboxes)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (onboardingRequest == null)
            {
                return Empty(GetOnboardingDetailsStatus.NotFound);
            }

            var user = _currentUserAccessor.User;
            if (!CompanyScopeHelper.CanAccessCompany(user, onboardingRequest.Branch))
            {
                return Empty(GetOnboardingDetailsStatus.Forbidden);
            }

            await _context.LogActionAsync(
                nameof(OnboardingRequest),
                request.Id.ToString(),
                "Read",
                $"Viewed onboarding details for: {onboardingRequest.FullName}");

            var currentUser = await _userManager.GetUserAsync(user);
            var currentUserSignature = currentUser?.Signature;

            var integrityError = VerifyIntegrity(onboardingRequest);
            var slaSetting = await _context.SlaSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.RequestType == "Onboarding", cancellationToken);
            var targetDays = slaSetting?.TargetDays ?? 3;
            var slaStart = onboardingRequest.RequesterSignedAt ?? onboardingRequest.RequestDate;
            var company = await ResolveCompanyAsync(onboardingRequest.Branch, cancellationToken);
            var companyLogo = string.IsNullOrWhiteSpace(company?.LogoPath) ? null : company.LogoPath;
            var slaDeadline = CompanyBusinessHoursHelper.CalculateSlaDeadline(
                company == null ? Array.Empty<CompanyEntity>() : [company],
                onboardingRequest.Branch,
                slaStart,
                targetDays);
            var viewModel = onboardingRequest.Adapt<OnboardingDetailsPageViewModel>();
            viewModel.IssuedAssets = onboardingRequest.IssuedAssets.Adapt<IReadOnlyList<RequestAssetReadModel>>();
            viewModel.IssuedSoftware = onboardingRequest.IssuedSoftware.Adapt<IReadOnlyList<RequestIssuedSoftwareReadModel>>();
            viewModel.RequestSoftware = onboardingRequest.RequestSoftware.Adapt<IReadOnlyList<RequestSoftwareReadModel>>();
            viewModel.RequestEmailGroups = onboardingRequest.RequestEmailGroups.Adapt<IReadOnlyList<RequestEmailGroupReadModel>>();
            viewModel.RequestSharedMailboxes = onboardingRequest.RequestSharedMailboxes.Adapt<IReadOnlyList<RequestSharedMailboxReadModel>>();
            viewModel.FolderPermissions = onboardingRequest.FolderPermissions.Adapt<IReadOnlyList<RequestFolderPermissionReadModel>>();
            viewModel.CurrentUserSignature = currentUserSignature;
            viewModel.SlaTargetDays = targetDays;
            viewModel.SlaStartTime = slaStart;
            viewModel.SlaDeadline = slaDeadline;
            viewModel.IntegrityError = integrityError;
            viewModel.CompanyLogoPath = companyLogo;
            viewModel.CompanyTimeZoneId = CompanyTimeZoneHelper.NormalizeTimeZoneId(company?.TimeZoneId);

            return new GetOnboardingDetailsResult(
                GetOnboardingDetailsStatus.Ready,
                viewModel);
        }

        private string? VerifyIntegrity(OnboardingRequest request)
        {
            try
            {
                if (request.RequesterSignedAt.HasValue && !string.IsNullOrEmpty(request.RequesterSignatureHmac) &&
                    !_integrityService.Verify("OnboardingRequest", request.Id.ToString(), request.RequesterSignerName ?? "Unknown", request.RequesterSignedAt.Value, request.RequesterSignature ?? string.Empty, request.RequesterSignatureHmac))
                {
                    return "Requester (HR) Signature Integrity Violation Detected.";
                }

                if (request.ITAdminSignedAt.HasValue && !string.IsNullOrEmpty(request.ITAdminSignatureHmac) &&
                    !_integrityService.Verify("OnboardingRequest", request.Id.ToString(), request.ITAdminSignerName ?? "Unknown", request.ITAdminSignedAt.Value, request.ITAdminSignature ?? string.Empty, request.ITAdminSignatureHmac))
                {
                    return "IT Admin Signature Integrity Violation Detected.";
                }

                if (request.EmployeeSignedAt.HasValue && !string.IsNullOrEmpty(request.EmployeeSignatureHmac) &&
                    !_integrityService.Verify("OnboardingRequest", request.Id.ToString(), request.EmployeeSignerName ?? "Employee", request.EmployeeSignedAt.Value, request.EmployeeSignature ?? string.Empty, request.EmployeeSignatureHmac))
                {
                    return "Employee Signature Integrity Violation Detected.";
                }
            }
            catch
            {
                return "Error during signature integrity verification.";
            }

            return null;
        }

        private async Task<CompanyEntity?> ResolveCompanyAsync(string? branchName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(branchName))
            {
                return null;
            }

            var companies = await _context.Companies
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return CompanyBusinessHoursHelper.ResolveCompany(companies, branchName);
        }

        private static GetOnboardingDetailsResult Empty(GetOnboardingDetailsStatus status)
            => new(status, null);
    }
}
