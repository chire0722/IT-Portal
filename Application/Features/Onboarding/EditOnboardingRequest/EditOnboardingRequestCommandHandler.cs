// Project comment: EditOnboardingRequestCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.EditOnboardingRequest
{
    /// <summary>
    /// Handles the EditOnboardingRequestCommand command workflow.
    /// </summary>
    public sealed class EditOnboardingRequestCommandHandler
        : IRequestHandler<EditOnboardingRequestCommand, EditOnboardingRequestResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly ILogger<EditOnboardingRequestCommandHandler> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public EditOnboardingRequestCommandHandler(
            ApplicationDbContext context,
            ISignatureIntegrityService integrityService,
            ILogger<EditOnboardingRequestCommandHandler> logger,
            UserManager<ApplicationUser> userManager,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _integrityService = integrityService;
            _logger = logger;
            _userManager = userManager;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<EditOnboardingRequestResult> Handle(EditOnboardingRequestCommand request, CancellationToken cancellationToken)
        {
            var currentUser = await _userManager.GetUserAsync(_currentUserAccessor.User);
            var userName = _currentUserAccessor.UserName;
            var userSignature = currentUser?.Signature;
            var userFullName = currentUser?.FullName;

            var onboardingRequest = await _context.OnboardingRequests
                .Include(r => r.IssuedAssets)
                .Include(r => r.FolderPermissions)
                .Include(r => r.RequestSoftware)
                .Include(r => r.RequestEmailGroups)
                .Include(r => r.RequestSharedMailboxes)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (onboardingRequest == null)
            {
                return new EditOnboardingRequestResult(
                    EditOnboardingRequestStatus.NotFound,
                    request.Id,
                    "Request not found.");
            }

            if ((!string.IsNullOrWhiteSpace(onboardingRequest.Branch) &&
                 !CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, onboardingRequest.Branch)) ||
                !CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, request.Model.Branch))
            {
                return new EditOnboardingRequestResult(
                    EditOnboardingRequestStatus.Forbidden,
                    request.Id,
                    "You can only edit onboarding requests within your assigned company scope.");
            }

            if (!onboardingRequest.CanBeEdited())
            {
                return new EditOnboardingRequestResult(
                    EditOnboardingRequestStatus.Locked,
                    request.Id,
                    "This request is locked and cannot be edited.");
            }

            EditOnboardingRequestSupport.ApplyCoreFields(onboardingRequest, request.Model);
            await EditOnboardingRequestSupport.ReplaceRequestedSoftwareAsync(_context, onboardingRequest, request.SelectedStandardSoftwareIds, cancellationToken);
            await EditOnboardingRequestSupport.ReplaceEmailGroupsAsync(_context, onboardingRequest, request.SelectedEmailGroupIds, cancellationToken);
            await EditOnboardingRequestSupport.ReplaceSharedMailboxesAsync(_context, onboardingRequest, request.SelectedSharedMailboxIds, cancellationToken);
            _context.IssuedAssets.RemoveRange(onboardingRequest.IssuedAssets);
            EditOnboardingRequestSupport.ReplaceIssuedAssets(onboardingRequest, request.Model.IssuedAssets);
            EditOnboardingRequestSupport.ApplyLegacySoftwareFlags(onboardingRequest, request.Model);
            onboardingRequest.UpdatedAt = DateTime.UtcNow;

            if (onboardingRequest.Status == RequestStatus.Draft && string.Equals(request.ActionType, "submit", StringComparison.OrdinalIgnoreCase))
            {
                var submittedAt = DateTime.UtcNow;
                string? signatureHmac = null;
                var resolvedSignature = userSignature ?? onboardingRequest.RequesterSignature;
                var signerName = !string.IsNullOrWhiteSpace(userFullName)
                    ? userFullName
                    : (userName ?? "Unknown");

                if (!string.IsNullOrEmpty(resolvedSignature))
                {
                    signatureHmac = _integrityService.ComputeHmac(
                        "OnboardingRequest",
                        onboardingRequest.Id.ToString(),
                        signerName,
                        submittedAt,
                        resolvedSignature);
                }

                try
                {
                    onboardingRequest.SubmitDraftByHr(resolvedSignature, signerName, signatureHmac, submittedAt);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ValidationException($"The onboarding request cannot be submitted from its current state. {ex.Message}");
                }
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Draft onboarding request #{Id} submitted for approval by {User}.", request.Id, userName);
                return new EditOnboardingRequestResult(
                    EditOnboardingRequestStatus.Submitted,
                    onboardingRequest.Id,
                    $"Draft for {onboardingRequest.FullName} has been submitted for approval!",
                    true);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Onboarding request #{Id} updated.", request.Id);

            return new EditOnboardingRequestResult(
                EditOnboardingRequestStatus.Updated,
                onboardingRequest.Id,
                $"Request for {onboardingRequest.FullName} updated successfully!");
        }
    }
}
