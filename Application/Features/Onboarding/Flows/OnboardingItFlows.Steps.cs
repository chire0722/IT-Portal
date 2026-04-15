using FluentValidation;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.GetOnboardingItStep3;
using IT_Form.Application.Features.Onboarding.GetOnboardingItStep4;
using IT_Form.Application.Features.Onboarding.GetOnboardingItSetup;
using IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep3;
using IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows
{
    public sealed record GetOnboardingStep3PageQuery() : IRequest<ProcessResult<OnboardingStep3PageViewModel>>;
    public sealed record StartOnboardingItSetupQuery(int Id) : IRequest<ProcessResult<object>>;
    public sealed record SubmitOnboardingStep3FlowCommand(OnboardingViewModel Model, string? SaveAction) : IRequest<ProcessResult<object>>;
    public sealed record GetOnboardingStep4PageQuery() : IRequest<ProcessResult<OnboardingViewModel>>;
    public sealed record SubmitOnboardingStep4FlowCommand(OnboardingViewModel Model, string? SaveAction) : IRequest<ProcessResult<object>>;

    public sealed class GetOnboardingStep3PageQueryHandler : IRequestHandler<GetOnboardingStep3PageQuery, ProcessResult<OnboardingStep3PageViewModel>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;

        public GetOnboardingStep3PageQueryHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
        }

        public async Task<ProcessResult<OnboardingStep3PageViewModel>> Handle(GetOnboardingStep3PageQuery request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetOnboardingItStep3Query(_sessionAccessor.Get()), cancellationToken);
            if (result.Status == GetOnboardingItStep3Status.SessionExpired)
                return ProcessResult<OnboardingStep3PageViewModel>.Failure(result.Message ?? "Session expired.", "Index");
            if (result.Status == GetOnboardingItStep3Status.NotFound)
                return ProcessResult<OnboardingStep3PageViewModel>.NotFound();
            if (result.Status == GetOnboardingItStep3Status.Forbidden)
                return ProcessResult<OnboardingStep3PageViewModel>.Forbidden();
            if (result.Status == GetOnboardingItStep3Status.Locked)
                return ProcessResult<OnboardingStep3PageViewModel>.Failure(result.Message ?? "IT setup can only be performed on pending requests.", "Details", result.RequestId);

            _sessionAccessor.Save(result.SessionModel!);
            return ProcessResult<OnboardingStep3PageViewModel>.Success(result.ViewModel!);
        }
    }

    public sealed class StartOnboardingItSetupQueryHandler : IRequestHandler<StartOnboardingItSetupQuery, ProcessResult<object>>
    {
        private readonly ISender _sender;
        private readonly IOnboardingSessionAccessor _sessionAccessor;

        public StartOnboardingItSetupQueryHandler(ISender sender, IOnboardingSessionAccessor sessionAccessor)
        {
            _sender = sender;
            _sessionAccessor = sessionAccessor;
        }

        public async Task<ProcessResult<object>> Handle(StartOnboardingItSetupQuery request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetOnboardingItSetupQuery(request.Id), cancellationToken);
            if (result.IsNotFound)
                return ProcessResult<object>.NotFound();
            if (result.IsForbidden)
                return ProcessResult<object>.Forbidden();
            if (!result.IsSuccess)
                return ProcessResult<object>.Failure(result.Message ?? "This request is not awaiting IT setup.", result.RedirectAction ?? "Details", result.RequestId > 0 ? result.RequestId : request.Id);

            _sessionAccessor.Save(result.Data!);
            return ProcessResult<object>.Success(redirectAction: "Step3");
        }
    }

    public sealed class SubmitOnboardingStep3FlowCommandHandler : IRequestHandler<SubmitOnboardingStep3FlowCommand, ProcessResult<object>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;

        public SubmitOnboardingStep3FlowCommandHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
        }

        public async Task<ProcessResult<object>> Handle(SubmitOnboardingStep3FlowCommand request, CancellationToken cancellationToken)
        {
            var session = _sessionAccessor.Get();
            if (session.Id == 0)
                return ProcessResult<object>.Failure("Session expired. Please use 'Continue IT Setup' from the request details page.", "Index");

            var result = await _sender.Send(
                new UpdateOnboardingItStep3Command(request.Model, session, string.Equals(request.SaveAction, "draft", StringComparison.OrdinalIgnoreCase)),
                cancellationToken);

            if (result.Status == UpdateOnboardingItStep3Status.NotFound)
                return ProcessResult<object>.NotFound();
            if (result.Status == UpdateOnboardingItStep3Status.Forbidden)
                return ProcessResult<object>.Forbidden();
            if (result.Status == UpdateOnboardingItStep3Status.Locked)
                return ProcessResult<object>.Failure(result.Message, "Details", result.RequestId);

            _sessionAccessor.Save(session);
            return result.Status == UpdateOnboardingItStep3Status.DraftSaved
                ? ProcessResult<object>.Success(message: result.Message, redirectAction: "Step3")
                : ProcessResult<object>.Success(redirectAction: "Step4");
        }
    }

    public sealed class GetOnboardingStep4PageQueryHandler : IRequestHandler<GetOnboardingStep4PageQuery, ProcessResult<OnboardingViewModel>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;

        public GetOnboardingStep4PageQueryHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
        }

        public async Task<ProcessResult<OnboardingViewModel>> Handle(GetOnboardingStep4PageQuery request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetOnboardingItStep4Query(_sessionAccessor.Get()), cancellationToken);
            if (result.Status == GetOnboardingItStep4Status.SessionExpired)
                return ProcessResult<OnboardingViewModel>.Failure(result.Message ?? "Session expired.", "Index");
            if (result.Status == GetOnboardingItStep4Status.NotFound)
                return ProcessResult<OnboardingViewModel>.NotFound();
            if (result.Status == GetOnboardingItStep4Status.Forbidden)
                return ProcessResult<OnboardingViewModel>.Forbidden();
            if (result.Status == GetOnboardingItStep4Status.Locked)
                return ProcessResult<OnboardingViewModel>.Failure(result.Message ?? "IT setup steps are locked.", "Details", result.RequestId);

            _sessionAccessor.Save(result.Model!);
            return ProcessResult<OnboardingViewModel>.Success(result.Model!);
        }
    }

    public sealed class SubmitOnboardingStep4FlowCommandHandler : IRequestHandler<SubmitOnboardingStep4FlowCommand, ProcessResult<object>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;

        public SubmitOnboardingStep4FlowCommandHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
        }

        public async Task<ProcessResult<object>> Handle(SubmitOnboardingStep4FlowCommand request, CancellationToken cancellationToken)
        {
            var session = _sessionAccessor.Get();
            if (session.Id == 0)
                return ProcessResult<object>.Failure("Session expired. Please use 'Continue IT Setup' from the request details page.", "Index");

            var result = await _sender.Send(
                new UpdateOnboardingItStep4Command(request.Model, session, string.Equals(request.SaveAction, "draft", StringComparison.OrdinalIgnoreCase)),
                cancellationToken);

            if (result.Status == UpdateOnboardingItStep4Status.NotFound)
                return ProcessResult<object>.NotFound();
            if (result.Status == UpdateOnboardingItStep4Status.Forbidden)
                return ProcessResult<object>.Forbidden();
            if (result.Status == UpdateOnboardingItStep4Status.Locked)
                return ProcessResult<object>.Failure(result.Message, "Details", result.RequestId);

            _sessionAccessor.Save(session);
            return result.Status == UpdateOnboardingItStep4Status.DraftSaved
                ? ProcessResult<object>.Success(message: result.Message, redirectAction: "Step4")
                : ProcessResult<object>.Success(redirectAction: "Step5");
        }
    }

    public sealed class SubmitOnboardingStep3FlowCommandValidator : AbstractValidator<SubmitOnboardingStep3FlowCommand>
    {
        public SubmitOnboardingStep3FlowCommandValidator() => RuleFor(x => x.Model).NotNull();
    }

    public sealed class SubmitOnboardingStep4FlowCommandValidator : AbstractValidator<SubmitOnboardingStep4FlowCommand>
    {
        public SubmitOnboardingStep4FlowCommandValidator() => RuleFor(x => x.Model).NotNull();
    }
}
