// Project comment: GetOffboardingDetailsQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Offboarding;
using IT_Form.Application.Features.Offboarding.ActionWrites;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = IT_Form.Models.Company;

namespace IT_Form.Application.Features.Offboarding.GetOffboardingDetails
{
    /// <summary>
    /// Handles the GetOffboardingDetailsQuery query workflow.
    /// </summary>
    public sealed class GetOffboardingDetailsQueryHandler : IRequestHandler<GetOffboardingDetailsQuery, ProcessResult<OffboardingDetailsPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetOffboardingDetailsQueryHandler(
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
        public async Task<ProcessResult<OffboardingDetailsPageViewModel>> Handle(GetOffboardingDetailsQuery request, CancellationToken cancellationToken)
        {
            var offboardingRequest = await _context.OffboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .Include(o => o.AssignedHod)
                .Include(o => o.OnboardingRequest)
                    .ThenInclude(or => or!.IssuedAssets)
                        .ThenInclude(ia => ia.ITAsset)
                .Include(o => o.OnboardingRequest)
                    .ThenInclude(or => or!.IssuedSoftware)
                .Include(o => o.OnboardingRequest)
                    .ThenInclude(or => or!.FolderPermissions)
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (offboardingRequest == null)
            {
                return ProcessResult<OffboardingDetailsPageViewModel>.NotFound();
            }

            if (!await OffboardingReadAccessSupport.IsUserAuthorizedForRequest(
                    _context,
                    _currentUserAccessor.User,
                    _userManager,
                    offboardingRequest,
                    _currentUserAccessor.UserName))
            {
                return ProcessResult<OffboardingDetailsPageViewModel>.Forbidden();
            }

            await _context.LogActionAsync(
                nameof(OffboardingRequest),
                request.Id.ToString(),
                "Read",
                $"Viewed offboarding details for: {offboardingRequest.FullName}");

            var currentUser = await _userManager.GetUserAsync(_currentUserAccessor.User);
            var latestHodEmail = await UserHelper.GetLatestHODEmailByCompanyAsync(_context, offboardingRequest.Branch, offboardingRequest.Department);
            var managerDisplayName = await ResolveManagerDisplayNameAsync(offboardingRequest, latestHodEmail, cancellationToken);
            var slaTargetDays = (await _context.SlaSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.RequestType == "Offboarding", cancellationToken))?.TargetDays ?? 3;
            var slaStart = offboardingRequest.HRSignedAt ?? offboardingRequest.RequestDate;
            var company = await ResolveCompanyAsync(offboardingRequest.Branch, cancellationToken);
            IEnumerable<CompanyEntity> slaCompanies = company is null
                ? Array.Empty<CompanyEntity>()
                : new[] { company };
            var viewModel = offboardingRequest.Adapt<OffboardingDetailsPageViewModel>();
            viewModel.OnboardingRequest = offboardingRequest.OnboardingRequest?.Adapt<OffboardingOnboardingSnapshotViewModel>();

            if (viewModel.OnboardingRequest != null && offboardingRequest.OnboardingRequest != null)
            {
                viewModel.OnboardingRequest.IssuedAssets = offboardingRequest.OnboardingRequest.IssuedAssets
                    .Adapt<IReadOnlyList<RequestAssetReadModel>>();
                viewModel.OnboardingRequest.IssuedSoftware = offboardingRequest.OnboardingRequest.IssuedSoftware
                    .Adapt<IReadOnlyList<RequestIssuedSoftwareReadModel>>();
                viewModel.OnboardingRequest.FolderPermissions = offboardingRequest.OnboardingRequest.FolderPermissions
                    .Adapt<IReadOnlyList<RequestFolderPermissionReadModel>>();
            }

            viewModel.CurrentUserSignature = currentUser?.Signature;
            viewModel.CurrentUserId = currentUser?.Id;
            viewModel.MyManagerEmails = await UserHelper.GetHODEmailsAsync(_context, _currentUserAccessor.UserName ?? string.Empty);
            viewModel.ManagerDisplayName = managerDisplayName;
            viewModel.SlaTargetDays = slaTargetDays;
            viewModel.SlaStartTime = slaStart;
            viewModel.SlaDeadline = CompanyBusinessHoursHelper.CalculateSlaDeadline(
                slaCompanies,
                offboardingRequest.Branch,
                slaStart,
                slaTargetDays);
            viewModel.IntegrityError = VerifyIntegrity(offboardingRequest);
            viewModel.CompanyLogoPath = string.IsNullOrWhiteSpace(company?.LogoPath) ? null : company.LogoPath;
            viewModel.CompanyTimeZoneId = CompanyTimeZoneHelper.NormalizeTimeZoneId(company?.TimeZoneId);

            return ProcessResult<OffboardingDetailsPageViewModel>.Success(viewModel);
        }

        /// <summary>
        /// Runs the ResolveManagerDisplayNameAsync operation.
        /// </summary>
        private async Task<string> ResolveManagerDisplayNameAsync(OffboardingRequest request, string? latestHodEmail, CancellationToken cancellationToken)
        {
            if (request.AssignedHod != null)
            {
                return !string.IsNullOrWhiteSpace(request.AssignedHod.FullName)
                    ? request.AssignedHod.FullName
                    : request.AssignedHod.Email ?? "-";
            }

            if (!string.IsNullOrWhiteSpace(request.HODEmail))
            {
                var managerUserByEmail = await _context.Users
                    .Where(u => u.IsActive && u.Email != null && u.Email.ToLower() == request.HODEmail.ToLower())
                    .Select(u => new { u.FullName, u.Email })
                    .FirstOrDefaultAsync(cancellationToken);

                if (managerUserByEmail != null)
                {
                    return !string.IsNullOrWhiteSpace(managerUserByEmail.FullName)
                        ? managerUserByEmail.FullName
                        : managerUserByEmail.Email ?? "-";
                }
            }

            if (!string.IsNullOrWhiteSpace(latestHodEmail))
            {
                var latestHodUser = await _context.Users
                    .Where(u => u.IsActive && u.Email != null && u.Email.ToLower() == latestHodEmail.ToLower())
                    .Select(u => new { u.FullName, u.Email })
                    .FirstOrDefaultAsync(cancellationToken);

                if (latestHodUser != null)
                {
                    return !string.IsNullOrWhiteSpace(latestHodUser.FullName)
                        ? latestHodUser.FullName
                        : latestHodUser.Email ?? "-";
                }
            }

            return latestHodEmail ?? request.HODEmail ?? "-";
        }

        /// <summary>
        /// Runs the VerifyIntegrity operation.
        /// </summary>
        private string? VerifyIntegrity(OffboardingRequest request)
        {
            var entityId = request.Id.ToString();
            var requiresHr = request.Status is RequestStatus.PendingApproval or RequestStatus.PendingHODApproval or RequestStatus.PendingITApproval or RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresHod = request.Status is RequestStatus.PendingITApproval or RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresIt = request.Status is RequestStatus.PendingEmployeeSign or RequestStatus.Completed or RequestStatus.Approved;
            var requiresEmployee = request.Status is RequestStatus.Completed or RequestStatus.Approved;

            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", entityId,
                    request.HRSignerName, request.HRSignedAt, request.HRSignature, request.HRSignatureHmac, isRequired: requiresHr))
                return "HR Signature Integrity Violation Detected or legacy signature cannot be trusted.";

            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", entityId,
                    request.HODSignerName, request.HODSignedAt, request.HODSignature, request.HODSignatureHmac, isRequired: requiresHod))
                return "HOD Signature Integrity Violation Detected or legacy signature cannot be trusted.";

            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", entityId,
                    request.ITAdminSignerName, request.ITAdminSignedAt, request.ITAdminSignature, request.ITAdminSignatureHmac, isRequired: requiresIt))
                return "IT Admin Signature Integrity Violation Detected or legacy signature cannot be trusted.";

            if (!SignatureIntegrityGuard.IsValid(_integrityService, "OffboardingRequest", entityId,
                    request.EmployeeSignerName, request.EmployeeSignedAt, request.EmployeeSignature, request.EmployeeSignatureHmac, "Employee", isRequired: requiresEmployee))
                return "Employee Signature Integrity Violation Detected or legacy signature cannot be trusted.";

            return null;
        }

        /// <summary>
        /// Runs the ResolveCompanyAsync operation.
        /// </summary>
        private async Task<CompanyEntity?> ResolveCompanyAsync(string? branchName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                return null;
            }

            var companies = await _context.Companies
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return CompanyBusinessHoursHelper.ResolveCompany(companies, branchName);
        }
    }
}
