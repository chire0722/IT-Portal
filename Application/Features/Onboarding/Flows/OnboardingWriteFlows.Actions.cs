using FluentValidation;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.ActionWrites;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows
{
    public sealed record ProcessOnboardingSignatureFlowCommand(int Id, string SignatureType, string SignatureData) : IRequest<ProcessResult<object>>;
    public sealed record ProcessOnboardingRejectionFlowCommand(int Id, string Reason) : IRequest<ProcessResult<object>>;
    public sealed record ProcessOnboardingTokenRegenerationFlowCommand(int Id) : IRequest<ProcessResult<object>>;

    public sealed class ProcessOnboardingSignatureFlowCommandHandler : IRequestHandler<ProcessOnboardingSignatureFlowCommand, ProcessResult<object>>
    {
        private readonly ISender _sender;
        public ProcessOnboardingSignatureFlowCommandHandler(ISender sender) => _sender = sender;
        public async Task<ProcessResult<object>> Handle(ProcessOnboardingSignatureFlowCommand request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new ProcessOnboardingSignatureCommand(request.Id, request.SignatureType, request.SignatureData), cancellationToken);
            return new ProcessResult<object> { IsSuccess = result.IsSuccess, IsNotFound = result.IsNotFound, IsForbidden = result.IsForbidden, Message = result.Message, RedirectAction = "Details", RequestId = result.RequestId > 0 ? result.RequestId : request.Id, AjaxAware = true };
        }
    }

    public sealed class ProcessOnboardingRejectionFlowCommandHandler : IRequestHandler<ProcessOnboardingRejectionFlowCommand, ProcessResult<object>>
    {
        private readonly ISender _sender;
        public ProcessOnboardingRejectionFlowCommandHandler(ISender sender) => _sender = sender;
        public async Task<ProcessResult<object>> Handle(ProcessOnboardingRejectionFlowCommand request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new ProcessOnboardingRejectionCommand(request.Id, request.Reason), cancellationToken);
            return new ProcessResult<object> { IsSuccess = result.IsSuccess, IsNotFound = result.IsNotFound, IsForbidden = result.IsForbidden, Message = result.Message, RedirectAction = "Details", RequestId = result.RequestId > 0 ? result.RequestId : request.Id, AjaxAware = true, AutoOpenEmail = result.IsSuccess ? $"ComposeRejectionEmail:{result.RequestId}:Onboarding" : null };
        }
    }

    public sealed class ProcessOnboardingTokenRegenerationFlowCommandHandler : IRequestHandler<ProcessOnboardingTokenRegenerationFlowCommand, ProcessResult<object>>
    {
        private readonly ISender _sender;
        public ProcessOnboardingTokenRegenerationFlowCommandHandler(ISender sender) => _sender = sender;
        public async Task<ProcessResult<object>> Handle(ProcessOnboardingTokenRegenerationFlowCommand request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new ProcessOnboardingTokenRegenerationCommand(request.Id), cancellationToken);
            return new ProcessResult<object> { IsSuccess = result.IsSuccess, IsNotFound = result.IsNotFound, IsForbidden = result.IsForbidden, Message = result.Message, RedirectAction = "Details", RequestId = result.RequestId > 0 ? result.RequestId : request.Id, AjaxAware = true };
        }
    }

    public sealed class ProcessOnboardingSignatureFlowCommandValidator : AbstractValidator<ProcessOnboardingSignatureFlowCommand> { public ProcessOnboardingSignatureFlowCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.SignatureType).NotEmpty(); RuleFor(x => x.SignatureData).NotEmpty(); } }
    public sealed class ProcessOnboardingRejectionFlowCommandValidator : AbstractValidator<ProcessOnboardingRejectionFlowCommand> { public ProcessOnboardingRejectionFlowCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Reason).NotEmpty(); } }
    public sealed class ProcessOnboardingTokenRegenerationFlowCommandValidator : AbstractValidator<ProcessOnboardingTokenRegenerationFlowCommand> { public ProcessOnboardingTokenRegenerationFlowCommandValidator() => RuleFor(x => x.Id).GreaterThan(0); }
}
