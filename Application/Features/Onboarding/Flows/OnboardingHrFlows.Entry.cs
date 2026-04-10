using FluentValidation;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.GetOnboardingStep1Options;
using IT_Form.Application.Features.Onboarding.SaveOnboardingDraft;
using IT_Form.Application.Features.Onboarding.SaveOnboardingStep1;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows
{
    public sealed record StartOnboardingCreateQuery() : IRequest<ProcessResult<object>>;
    public sealed record GetOnboardingStep1PageQuery() : IRequest<ProcessResult<OnboardingStep1PageViewModel>>;
    public sealed record SubmitOnboardingStep1FlowCommand(OnboardingStep1PageViewModel Model) : IRequest<ProcessResult<object>>;
    public sealed record OpenOnboardingDraftEntryQuery() : IRequest<ProcessResult<object>>;
    public sealed record SubmitOnboardingDraftFlowCommand(OnboardingViewModel Model) : IRequest<ProcessResult<object>>;

    public sealed class StartOnboardingCreateQueryHandler : IRequestHandler<StartOnboardingCreateQuery, ProcessResult<object>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        public StartOnboardingCreateQueryHandler(IOnboardingSessionAccessor sessionAccessor) => _sessionAccessor = sessionAccessor;
        public async Task<ProcessResult<object>> Handle(StartOnboardingCreateQuery request, CancellationToken cancellationToken)
        {
            _sessionAccessor.Save(await _sessionAccessor.CreateNewAsync(cancellationToken));
            return ProcessResult<object>.Success(redirectAction: "Step1");
        }
    }

    public sealed class GetOnboardingStep1PageQueryHandler : IRequestHandler<GetOnboardingStep1PageQuery, ProcessResult<OnboardingStep1PageViewModel>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;

        public GetOnboardingStep1PageQueryHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
        }

        public async Task<ProcessResult<OnboardingStep1PageViewModel>> Handle(GetOnboardingStep1PageQuery request, CancellationToken cancellationToken)
        {
            var model = _sessionAccessor.Get();
            model.CurrentStep = 1;
            return ProcessResult<OnboardingStep1PageViewModel>.Success(
                await _sender.Send(new GetOnboardingStep1OptionsQuery(model), cancellationToken));
        }
    }

    public sealed class SubmitOnboardingStep1FlowCommandHandler : IRequestHandler<SubmitOnboardingStep1FlowCommand, ProcessResult<object>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;

        public SubmitOnboardingStep1FlowCommandHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
        }

        public async Task<ProcessResult<object>> Handle(SubmitOnboardingStep1FlowCommand request, CancellationToken cancellationToken)
        {
            var session = _sessionAccessor.Get();
            var result = await _sender.Send(new SaveOnboardingStep1Command(request.Model, session), cancellationToken);
            if (result.Status == SaveOnboardingStep1Status.Locked)
                return ProcessResult<object>.Failure(result.Message, "Details", result.RequestId);
            if (result.Status == SaveOnboardingStep1Status.Forbidden)
                return ProcessResult<object>.Forbidden();

            _sessionAccessor.Save(session);
            return ProcessResult<object>.Success(message: result.Message, redirectAction: "Step2", requestId: result.RequestId);
        }
    }

    public sealed class OpenOnboardingDraftEntryQueryHandler : IRequestHandler<OpenOnboardingDraftEntryQuery, ProcessResult<object>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        public OpenOnboardingDraftEntryQueryHandler(IOnboardingSessionAccessor sessionAccessor) => _sessionAccessor = sessionAccessor;
        public Task<ProcessResult<object>> Handle(OpenOnboardingDraftEntryQuery request, CancellationToken cancellationToken)
        {
            var session = _sessionAccessor.Get();
            return Task.FromResult(ProcessResult<object>.Failure(
                "Draft links must be submitted from the onboarding wizard.",
                session.CurrentStep >= 2 ? "Step2" : "Step1"));
        }
    }

    public sealed class SubmitOnboardingDraftFlowCommandHandler : IRequestHandler<SubmitOnboardingDraftFlowCommand, ProcessResult<object>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public SubmitOnboardingDraftFlowCommandHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender, ICurrentUserAccessor currentUserAccessor)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<object>> Handle(SubmitOnboardingDraftFlowCommand request, CancellationToken cancellationToken)
        {
            if (request.Model.CurrentStep >= 2)
                return await _sender.Send(new SubmitOnboardingStep2FlowCommand(request.Model), cancellationToken);

            var session = await _sessionAccessor.PrepareDraftAsync(_sessionAccessor.Get(), request.Model, cancellationToken);
            _sessionAccessor.Save(session);

            var result = await _sender.Send(new SaveOnboardingDraftCommand(session, _currentUserAccessor.UserName), cancellationToken);
            if (result.Status == SaveOnboardingDraftStatus.NotFound)
                return ProcessResult<object>.NotFound();
            if (result.Status == SaveOnboardingDraftStatus.InvalidState)
                return ProcessResult<object>.Failure(result.Message, "Details", result.RequestId);
            if (result.Status == SaveOnboardingDraftStatus.Forbidden)
                return ProcessResult<object>.Forbidden();

            session.Id = result.RequestId;
            _sessionAccessor.Save(session);
            return ProcessResult<object>.Success(message: result.Message, redirectAction: "Step1");
        }
    }

    public sealed class SubmitOnboardingStep1FlowCommandValidator : AbstractValidator<SubmitOnboardingStep1FlowCommand>
    {
        public SubmitOnboardingStep1FlowCommandValidator() => RuleFor(x => x.Model).NotNull();
    }

    public sealed class SubmitOnboardingDraftFlowCommandValidator : AbstractValidator<SubmitOnboardingDraftFlowCommand>
    {
        public SubmitOnboardingDraftFlowCommandValidator() => RuleFor(x => x.Model).NotNull();
    }
}
