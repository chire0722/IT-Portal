using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.SignOnboardingRequest
{
    public sealed class SignOnboardingRequestCommandHandler : IRequestHandler<SignOnboardingRequestCommand, SignOnboardingRequestResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly ILogger<SignOnboardingRequestCommandHandler> _logger;

        public SignOnboardingRequestCommandHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            UserManager<ApplicationUser> userManager,
            ISignatureIntegrityService integrityService,
            ILogger<SignOnboardingRequestCommandHandler> logger)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
            _integrityService = integrityService;
            _logger = logger;
        }

        public async Task<SignOnboardingRequestResult> Handle(SignOnboardingRequestCommand request, CancellationToken cancellationToken)
        {
            var onboardingRequest = await _context.OnboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (onboardingRequest == null)
            {
                return new SignOnboardingRequestResult(
                    SignOnboardingRequestStatus.NotFound,
                    request.Id,
                    "Request not found.");
            }

            var user = _currentUserAccessor.User;
            var currentUsername = _currentUserAccessor.UserName;

            if (!CompanyScopeHelper.CanAccessCompany(user, onboardingRequest.Branch))
            {
                return new SignOnboardingRequestResult(
                    SignOnboardingRequestStatus.ForbiddenScope,
                    request.Id,
                    "You are not authorized for this request.");
            }

            if (!onboardingRequest.CanBeItSigned())
            {
                return new SignOnboardingRequestResult(
                    SignOnboardingRequestStatus.InvalidState,
                    request.Id,
                    "This request is not in the correct state for IT signing.");
            }

            if (!string.Equals(request.SignatureType, "ITAdmin", StringComparison.Ordinal))
            {
                return new SignOnboardingRequestResult(
                    SignOnboardingRequestStatus.ForbiddenRole,
                    request.Id,
                    "Unsupported signature type.");
            }

            if (!user.HasClaim("UserTitle", "IT"))
            {
                return new SignOnboardingRequestResult(
                    SignOnboardingRequestStatus.ForbiddenRole,
                    request.Id,
                    "Only users with IT title can sign as IT Admin.");
            }

            var message = "Signed successfully.";
            if (string.IsNullOrEmpty(onboardingRequest.ITAdminSignature))
            {
                var signer = await _userManager.GetUserAsync(user);
                var signerName = !string.IsNullOrWhiteSpace(signer?.FullName)
                    ? signer.FullName
                    : (currentUsername ?? "Unknown");
                var signedAt = DateTime.UtcNow;
                var signatureHmac = _integrityService.ComputeHmac(
                    "OnboardingRequest",
                    onboardingRequest.Id.ToString(),
                    signerName,
                    signedAt,
                    request.SignatureData);

                onboardingRequest.SignByItAdmin(request.SignatureData, signerName, signatureHmac, signedAt);
                message = "IT Admin signature added! Generating employee acknowledgement email...";
                _logger.LogInformation("IT Admin signed request #{Id}", onboardingRequest.Id);
            }
            await _context.SaveChangesAsync(cancellationToken);

            return new SignOnboardingRequestResult(
                SignOnboardingRequestStatus.Signed,
                onboardingRequest.Id,
                message);
        }
    }
}
