// Project comment: OffboardingActionWriteValidators.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Helpers;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Validates input for the ProcessOffboardingHrSignatureCommand request.
    /// </summary>
    public sealed class ProcessOffboardingHrSignatureCommandValidator : AbstractValidator<ProcessOffboardingHrSignatureCommand>
    {
        public ProcessOffboardingHrSignatureCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64).NotEmpty().WithMessage("Signature data is invalid or too large.").Must(SignatureDataUriValidator.IsValid).When(x => !string.IsNullOrWhiteSpace(x.SignatureBase64)).WithMessage("Signature must be a valid PNG image within the allowed size and dimensions.");
        }
    }

    /// <summary>
    /// Validates input for the ProcessOffboardingCheckerApprovalCommand request.
    /// </summary>
    public sealed class ProcessOffboardingCheckerApprovalCommandValidator : AbstractValidator<ProcessOffboardingCheckerApprovalCommand>
    {
        public ProcessOffboardingCheckerApprovalCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64).Must(SignatureDataUriValidator.IsValid).WithMessage("Signature must be a valid PNG image within the allowed size and dimensions.");
        }
    }

    /// <summary>
    /// Validates input for the ProcessOffboardingManagerSignatureCommand request.
    /// </summary>
    public sealed class ProcessOffboardingManagerSignatureCommandValidator : AbstractValidator<ProcessOffboardingManagerSignatureCommand>
    {
        public ProcessOffboardingManagerSignatureCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64).Must(SignatureDataUriValidator.IsValid).WithMessage("Signature must be a valid PNG image within the allowed size and dimensions.");
        }
    }

    /// <summary>
    /// Validates input for the ProcessOffboardingItSignatureCommand request.
    /// </summary>
    public sealed class ProcessOffboardingItSignatureCommandValidator : AbstractValidator<ProcessOffboardingItSignatureCommand>
    {
        public ProcessOffboardingItSignatureCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64).Must(SignatureDataUriValidator.IsValid).WithMessage("Signature must be a valid PNG image within the allowed size and dimensions.");
        }
    }

    /// <summary>
    /// Validates input for the ProcessOffboardingEmployeeManualSignatureCommand request.
    /// </summary>
    public sealed class ProcessOffboardingEmployeeManualSignatureCommandValidator : AbstractValidator<ProcessOffboardingEmployeeManualSignatureCommand>
    {
        public ProcessOffboardingEmployeeManualSignatureCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64).Must(SignatureDataUriValidator.IsValid).WithMessage("Signature must be a valid PNG image within the allowed size and dimensions.");
        }
    }

    /// <summary>
    /// Validates input for the ProcessOffboardingRejectionCommand request.
    /// </summary>
    public sealed class ProcessOffboardingRejectionCommandValidator : AbstractValidator<ProcessOffboardingRejectionCommand>
    {
        public ProcessOffboardingRejectionCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Reason).NotEmpty().WithMessage("A rejection reason is required.");
        }
    }

    /// <summary>
    /// Validates input for the ProcessOffboardingTokenRegenerationCommand request.
    /// </summary>
    public sealed class ProcessOffboardingTokenRegenerationCommandValidator : AbstractValidator<ProcessOffboardingTokenRegenerationCommand>
    {
        public ProcessOffboardingTokenRegenerationCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
