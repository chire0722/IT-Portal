// Project comment: OnboardingItFlows.Completion.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup;
using IT_Form.Application.Features.Onboarding.GetOnboardingItStep5;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingStep5Page query.
    /// </summary>
    public sealed record GetOnboardingStep5PageQuery() : IRequest<ProcessResult<OnboardingStep5PageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the SubmitOnboardingStep5Flow command.
    /// </summary>
    public sealed record SubmitOnboardingStep5FlowCommand(CompleteOnboardingItSetupCommand Command) : IRequest<ProcessResult<object>>;

    /// <summary>
    /// Handles the GetOnboardingStep5PageQuery query workflow.
    /// </summary>
    public sealed class GetOnboardingStep5PageQueryHandler : IRequestHandler<GetOnboardingStep5PageQuery, ProcessResult<OnboardingStep5PageViewModel>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;

        public GetOnboardingStep5PageQueryHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<OnboardingStep5PageViewModel>> Handle(GetOnboardingStep5PageQuery request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetOnboardingItStep5Query(_sessionAccessor.Get()), cancellationToken);
            return result.Status == GetOnboardingItStep5Status.SessionExpired
                ? ProcessResult<OnboardingStep5PageViewModel>.Failure(result.Message ?? "Session expired.", "Index")
                : ProcessResult<OnboardingStep5PageViewModel>.Success(result.ViewModel!);
        }
    }

    /// <summary>
    /// Handles the SubmitOnboardingStep5FlowCommand command workflow.
    /// </summary>
    public sealed class SubmitOnboardingStep5FlowCommandHandler : IRequestHandler<SubmitOnboardingStep5FlowCommand, ProcessResult<object>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;

        public SubmitOnboardingStep5FlowCommandHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(SubmitOnboardingStep5FlowCommand request, CancellationToken cancellationToken)
        {
            var session = _sessionAccessor.Get();
            if (session.Id == 0)
                return ProcessResult<object>.Failure("Session expired. Please use 'Continue IT Setup' from the request details page.", "Index");

            request.Command.Session = session;
            var result = await _sender.Send(request.Command, cancellationToken);
            if (result.Status == CompleteOnboardingItSetupStatus.NotFound)
                return ProcessResult<object>.NotFound();
            if (result.Status == CompleteOnboardingItSetupStatus.ChecklistIncomplete)
                return ProcessResult<object>.Failure(result.Message, "Step5");

            _sessionAccessor.Save(session);
            if (result.Status == CompleteOnboardingItSetupStatus.DraftSaved)
                return ProcessResult<object>.Success(message: result.Message, redirectAction: "Step5");

            _sessionAccessor.Remove();
            return new ProcessResult<object>
            {
                IsSuccess = true,
                Message = result.Message,
                RedirectAction = "Details",
                RequestId = result.RequestId,
                WarningMessage = result.WarningMessage,
                ShowItSummary = true
            };
        }
    }

    /// <summary>
    /// Validates input for the SubmitOnboardingStep5FlowCommand request.
    /// </summary>
    public sealed class SubmitOnboardingStep5FlowCommandValidator : AbstractValidator<SubmitOnboardingStep5FlowCommand>
    {
        public SubmitOnboardingStep5FlowCommandValidator() => RuleFor(x => x.Command).NotNull();
    }
}
