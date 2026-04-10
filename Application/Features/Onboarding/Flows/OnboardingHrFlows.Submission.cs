using FluentValidation;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows
{
    public sealed record GetOnboardingStep2PageQuery() : IRequest<ProcessResult<OnboardingViewModel>>;
    public sealed record SubmitOnboardingStep2FlowCommand(OnboardingViewModel Model) : IRequest<ProcessResult<object>>;

    public sealed class GetOnboardingStep2PageQueryHandler : IRequestHandler<GetOnboardingStep2PageQuery, ProcessResult<OnboardingViewModel>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        public GetOnboardingStep2PageQueryHandler(IOnboardingSessionAccessor sessionAccessor) => _sessionAccessor = sessionAccessor;
        public Task<ProcessResult<OnboardingViewModel>> Handle(GetOnboardingStep2PageQuery request, CancellationToken cancellationToken) =>
            Task.FromResult(ProcessResult<OnboardingViewModel>.Success(_sessionAccessor.PrepareStep2(_sessionAccessor.Get())));
    }

    public sealed class SubmitOnboardingStep2FlowCommandHandler : IRequestHandler<SubmitOnboardingStep2FlowCommand, ProcessResult<object>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public SubmitOnboardingStep2FlowCommandHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender, ICurrentUserAccessor currentUserAccessor)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<object>> Handle(SubmitOnboardingStep2FlowCommand request, CancellationToken cancellationToken)
        {
            var session = _sessionAccessor.Get();
            var result = await _sender.Send(new CompleteOnboardingHrStep2Command(request.Model, session, _currentUserAccessor.UserName), cancellationToken);
            if (result.Status == CompleteOnboardingHrStep2Status.NotFound)
                return ProcessResult<object>.NotFound();
            if (result.Status == CompleteOnboardingHrStep2Status.MissingStep1)
                return ProcessResult<object>.Failure(result.Message, "Step1");
            if (result.Status == CompleteOnboardingHrStep2Status.Locked)
                return ProcessResult<object>.Failure(result.Message, "Details", result.RequestId);
            if (result.Status == CompleteOnboardingHrStep2Status.Forbidden)
                return ProcessResult<object>.Forbidden();

            _sessionAccessor.Save(session);
            return ProcessResult<object>.Success(message: result.Message, redirectAction: "Details", requestId: result.RequestId);
        }
    }

    public sealed class SubmitOnboardingStep2FlowCommandValidator : AbstractValidator<SubmitOnboardingStep2FlowCommand>
    {
        public SubmitOnboardingStep2FlowCommandValidator() => RuleFor(x => x.Model).NotNull();
    }
}
