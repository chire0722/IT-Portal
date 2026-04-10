using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    public sealed class ProcessOffboardingHrSignatureCommandValidator : AbstractValidator<ProcessOffboardingHrSignatureCommand>
    {
        public ProcessOffboardingHrSignatureCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64).NotEmpty().MaximumLength(500000).WithMessage("Signature data is invalid or too large.");
        }
    }

    public sealed class ProcessOffboardingCheckerApprovalCommandValidator : AbstractValidator<ProcessOffboardingCheckerApprovalCommand>
    {
        public ProcessOffboardingCheckerApprovalCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64).NotEmpty().MaximumLength(500000).WithMessage("Signature data is invalid or too large.");
        }
    }

    public sealed class ProcessOffboardingManagerSignatureCommandValidator : AbstractValidator<ProcessOffboardingManagerSignatureCommand>
    {
        public ProcessOffboardingManagerSignatureCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64).NotEmpty().MaximumLength(500000).WithMessage("Signature data is invalid or too large.");
        }
    }

    public sealed class ProcessOffboardingItSignatureCommandValidator : AbstractValidator<ProcessOffboardingItSignatureCommand>
    {
        public ProcessOffboardingItSignatureCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64).NotEmpty().MaximumLength(500000).WithMessage("Signature data is invalid or too large.");
        }
    }

    public sealed class ProcessOffboardingEmployeeManualSignatureCommandValidator : AbstractValidator<ProcessOffboardingEmployeeManualSignatureCommand>
    {
        public ProcessOffboardingEmployeeManualSignatureCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64).NotEmpty().MaximumLength(500000).WithMessage("Signature data is invalid or too large.");
        }
    }

    public sealed class ProcessOffboardingRejectionCommandValidator : AbstractValidator<ProcessOffboardingRejectionCommand>
    {
        public ProcessOffboardingRejectionCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Reason).NotEmpty().WithMessage("A rejection reason is required.");
        }
    }

    public sealed class ProcessOffboardingTokenRegenerationCommandValidator : AbstractValidator<ProcessOffboardingTokenRegenerationCommand>
    {
        public ProcessOffboardingTokenRegenerationCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
