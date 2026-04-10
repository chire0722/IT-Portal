using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.SignOffHrOnboarding
{
    public sealed class SignOffHrOnboardingCommandHandler
        : IRequestHandler<SignOffHrOnboardingCommand, ProcessResult<object>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ISignatureIntegrityService _integrityService;
        private readonly ILogger<SignOffHrOnboardingCommandHandler> _logger;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public SignOffHrOnboardingCommandHandler(
            ApplicationDbContext context,
            ISignatureIntegrityService integrityService,
            ILogger<SignOffHrOnboardingCommandHandler> logger,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _integrityService = integrityService;
            _logger = logger;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<object>> Handle(SignOffHrOnboardingCommand request, CancellationToken cancellationToken)
        {
            var onboardingRequest = await _context.OnboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (onboardingRequest == null)
            {
                return ProcessResult<object>.NotFound();
            }

            if (!onboardingRequest.CanBeHrSignedOff())
            {
                return ProcessResult<object>.Failure("This request is not in a draft state and cannot be signed by HR.", "Details", onboardingRequest.Id);
            }

            var signedAt = DateTime.UtcNow;
            var signerName = string.IsNullOrWhiteSpace(_currentUserAccessor.UserName) ? "Unknown" : _currentUserAccessor.UserName;
            var signatureHmac = _integrityService.ComputeHmac(
                "OnboardingRequest",
                onboardingRequest.Id.ToString(),
                signerName,
                signedAt,
                request.SignatureBase64);

            try
            {
                onboardingRequest.SignOffByHr(request.SignatureBase64, signerName, signatureHmac, signedAt);
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException($"The onboarding request cannot be submitted from its current state. {ex.Message}");
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("HR manually signed Onboarding request #{Id} ({Name})", onboardingRequest.Id, onboardingRequest.FullName);

            return new ProcessResult<object>
            {
                IsSuccess = true,
                Message = "Signature saved! The request has been submitted to IT.",
                RedirectAction = "Details",
                RequestId = onboardingRequest.Id,
                AutoOpenEmail = $"ComposeEmail:{onboardingRequest.Id}"
            };
        }
    }
}
