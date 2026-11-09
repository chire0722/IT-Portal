// Project comment: GetOnboardingDetailsQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
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
    /// <summary>
    /// Handles the GetOnboardingDetailsQuery query workflow.
    /// </summary>
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

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
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
            if (!HrRecordReadCompanyScopeSupport.CanAccessCompany(user, onboardingRequest.Branch))
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

        /// <summary>
        /// Runs the VerifyIntegrity operation.
        /// </summary>
        private string? VerifyIntegrity(OnboardingRequest request)
        {
            var entityId = request.Id.ToString();
            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", entityId,
                    request.RequesterSignerName, request.RequesterSignedAt, request.RequesterSignature, request.RequesterSignatureHmac))
                return "Requester (HR) Signature Integrity Violation Detected or legacy signature cannot be trusted.";

            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", entityId,
                    request.ITAdminSignerName, request.ITAdminSignedAt, request.ITAdminSignature, request.ITAdminSignatureHmac))
                return "IT Admin Signature Integrity Violation Detected or legacy signature cannot be trusted.";

            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OnboardingRequest", entityId,
                    request.EmployeeSignerName, request.EmployeeSignedAt, request.EmployeeSignature, request.EmployeeSignatureHmac, "Employee"))
                return "Employee Signature Integrity Violation Detected or legacy signature cannot be trusted.";

            return null;
        }

        /// <summary>
        /// Runs the ResolveCompanyAsync operation.
        /// </summary>
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

        /// <summary>
        /// Runs the Empty operation.
        /// </summary>
        private static GetOnboardingDetailsResult Empty(GetOnboardingDetailsStatus status)
            => new(status, null);
    }
}
