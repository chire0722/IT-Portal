using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Models;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup
{
    public sealed class CompleteOnboardingItSetupCommandHandler
        : IRequestHandler<CompleteOnboardingItSetupCommand, CompleteOnboardingItSetupResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompleteOnboardingItSetupCommandHandler> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ISignatureIntegrityService _integrityService;

        public CompleteOnboardingItSetupCommandHandler(
            ApplicationDbContext context,
            ILogger<CompleteOnboardingItSetupCommandHandler> logger,
            UserManager<ApplicationUser> userManager,
            ICurrentUserAccessor currentUserAccessor,
            ISignatureIntegrityService integrityService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _currentUserAccessor = currentUserAccessor;
            _integrityService = integrityService;
        }

        public async Task<CompleteOnboardingItSetupResult> Handle(CompleteOnboardingItSetupCommand request, CancellationToken cancellationToken)
        {
            var session = request.Session;
            var currentUser = await _userManager.GetUserAsync(_currentUserAccessor.User);
            OnboardingItSetupSupport.ApplyChecklistToSession(session, request);

            var onboardingRequest = await _context.OnboardingRequests
                .Include(r => r.IssuedAssets)
                .Include(r => r.FolderPermissions)
                .Include(r => r.RequestSoftware)
                .Include(r => r.RequestEmailGroups)
                .Include(r => r.RequestSharedMailboxes)
                .FirstOrDefaultAsync(r => r.Id == session.Id, cancellationToken);

            if (onboardingRequest == null)
            {
                return new CompleteOnboardingItSetupResult(
                    CompleteOnboardingItSetupStatus.NotFound,
                    session.Id,
                    "Request not found.");
            }

            onboardingRequest.ApplyItChecklist(
                request.CheckPhysicalClean,
                request.CheckO365Setup,
                request.CheckWindowsLogin,
                request.CheckEmailSetup,
                request.CheckSpecificSoftware,
                request.CheckGeneralSoftware,
                request.CheckAbcGroup,
                request.CheckEmailMailGroup,
                request.CheckDomainJoin,
                request.CheckSharedDrive,
                request.CheckWiFiSetup,
                request.CheckPrinterSetup,
                request.CheckScanFolder,
                request.CheckVPNSetup,
                request.ChecklistRemarks,
                DateTime.UtcNow);

            if (!onboardingRequest.HasCompleteItChecklist() && !request.IsDraft)
            {
                return new CompleteOnboardingItSetupResult(
                    CompleteOnboardingItSetupStatus.ChecklistIncomplete,
                    session.Id,
                    "All IT Checklist items must be completed before submitting.");
            }

            if (request.IsDraft)
            {
                await OnboardingItSetupPersistence.SaveDraftAsync(_context, onboardingRequest, session, cancellationToken);
                return new CompleteOnboardingItSetupResult(
                    CompleteOnboardingItSetupStatus.DraftSaved,
                    onboardingRequest.Id,
                    "Draft saved successfully.");
            }

            var softwareAssignments = await OnboardingItSetupSupport.RebuildSoftwareAssignmentsAsync(
                _context,
                onboardingRequest,
                session,
                cancellationToken);
            await OnboardingItSetupSupport.SyncIssuedAssetsAsync(
                _context,
                onboardingRequest,
                session,
                cancellationToken);

            var signedAt = DateTime.UtcNow;
            var signerName = !string.IsNullOrWhiteSpace(currentUser?.FullName)
                ? currentUser.FullName
                : (currentUser?.UserName ?? _currentUserAccessor.UserName ?? "Unknown");
            var signatureHmac = !string.IsNullOrWhiteSpace(currentUser?.Signature)
                ? _integrityService.ComputeHmac(
                    "OnboardingRequest",
                    onboardingRequest.Id.ToString(),
                    signerName,
                    signedAt,
                    currentUser.Signature)
                : null;

            onboardingRequest.CompleteItSetup(currentUser?.Signature, signerName, signatureHmac, signedAt);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("IT Admin completed onboarding setup for request #{Id} ({Name})", onboardingRequest.Id, onboardingRequest.FullName);

            var successMessage = OnboardingItSetupSupport.BuildSuccessMessage(
                onboardingRequest.FullName ?? "Unknown",
                softwareAssignments.AssignedSoftwareNames);
            var warningMessage = OnboardingItSetupSupport.BuildWarningMessage(
                softwareAssignments.OutOfStockSoftwareNames);

            return new CompleteOnboardingItSetupResult(
                CompleteOnboardingItSetupStatus.Completed,
                onboardingRequest.Id,
                successMessage,
                warningMessage);
        }

    }
}
