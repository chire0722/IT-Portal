// Project comment: CompleteOnboardingItSetupCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup
{
    /// <summary>
    /// Handles saving draft IT checklist data or completing the onboarding IT setup workflow.
    /// </summary>
    public sealed class CompleteOnboardingItSetupCommandHandler
        : IRequestHandler<CompleteOnboardingItSetupCommand, CompleteOnboardingItSetupResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompleteOnboardingItSetupCommandHandler> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ISignatureIntegrityService _integrityService;

        /// <summary>
        /// Creates the handler with persistence, identity, current-user, and signature integrity services.
        /// </summary>
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

        /// <summary>
        /// Applies checklist data, saves drafts when requested, or completes the IT setup and signature step.
        /// </summary>
        public async Task<CompleteOnboardingItSetupResult> Handle(CompleteOnboardingItSetupCommand request, CancellationToken cancellationToken)
        {
            var session = request.Session;
            var currentUser = await _userManager.GetUserAsync(_currentUserAccessor.User);

            // Keep the session model synchronized with the submitted checklist values before persistence.
            OnboardingItSetupSupport.ApplyChecklistToSession(session, request);

            // Load the full aggregate because completion can update related issued assets and access requests.
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

            if (!_currentUserAccessor.User.HasClaim("UserTitle", "IT") ||
                !CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, onboardingRequest.Branch))
            {
                return new CompleteOnboardingItSetupResult(
                    CompleteOnboardingItSetupStatus.Forbidden,
                    session.Id,
                    "You are not authorized to complete IT setup for this company.");
            }

            if (onboardingRequest.Status != RequestStatus.PendingITApproval)
            {
                return new CompleteOnboardingItSetupResult(
                    CompleteOnboardingItSetupStatus.InvalidState,
                    session.Id,
                    "IT setup can only be changed while the request is pending IT approval.");
            }

            // Copy the submitted checklist state onto the tracked onboarding request entity.
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

            // Final submission requires every IT checklist item; drafts can stay incomplete.
            if (!onboardingRequest.HasCompleteItChecklist() && !request.IsDraft)
            {
                return new CompleteOnboardingItSetupResult(
                    CompleteOnboardingItSetupStatus.ChecklistIncomplete,
                    session.Id,
                    "All IT Checklist items must be completed before submitting.");
            }

            if (request.IsDraft)
            {
                // Draft persistence avoids completing signatures or issuing final software assignments.
                await OnboardingItSetupPersistence.SaveDraftAsync(_context, onboardingRequest, session, cancellationToken);
                return new CompleteOnboardingItSetupResult(
                    CompleteOnboardingItSetupStatus.DraftSaved,
                    onboardingRequest.Id,
                    "Draft saved successfully.");
            }

            var assetValidationMessage = await OnboardingItSetupSupport.ValidateSelectedAssetsAsync(
                _context,
                onboardingRequest,
                session,
                cancellationToken);
            if (assetValidationMessage != null)
            {
                return new CompleteOnboardingItSetupResult(
                    CompleteOnboardingItSetupStatus.InvalidAssetSelection,
                    onboardingRequest.Id,
                    assetValidationMessage);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Rebuild final software assignments from the current session selections.
            var softwareAssignments = await OnboardingItSetupSupport.RebuildSoftwareAssignmentsAsync(
                _context,
                onboardingRequest,
                session,
                cancellationToken);

            // Align issued asset records with the final hardware selections.
            var assetSyncError = await OnboardingItSetupSupport.SyncIssuedAssetsAsync(
                _context,
                onboardingRequest,
                session,
                cancellationToken);

            if (assetSyncError != null)
            {
                return new CompleteOnboardingItSetupResult(
                    CompleteOnboardingItSetupStatus.InvalidAssetSelection,
                    onboardingRequest.Id,
                    assetSyncError);
            }

            var signedAt = DateTime.UtcNow;

            // Prefer the user's full name for the signature display, falling back to account identifiers.
            var signerName = !string.IsNullOrWhiteSpace(currentUser?.FullName)
                ? currentUser.FullName
                : (currentUser?.UserName ?? _currentUserAccessor.UserName ?? "Unknown");

            // Bind the stored signature image to this request so later tampering can be detected.
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
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("IT Admin completed onboarding setup for request #{Id} ({Name})", onboardingRequest.Id, onboardingRequest.FullName);

            var successMessage = OnboardingItSetupSupport.BuildSuccessMessage(
                onboardingRequest.FullName ?? "Unknown",
                softwareAssignments.AssignedSoftwareNames);

            // Warnings are returned separately so the UI can show success and stock issues together.
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

