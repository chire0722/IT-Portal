// Project comment: OnboardingWriteFlows.Changes.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest;
using IT_Form.Application.Features.Onboarding.EditOnboardingRequest;
using IT_Form.Application.Features.Onboarding.SignOffHrOnboarding;
using IT_Form.Application.Features.Onboarding.UpdateManageAccess;
using IT_Form.Application.Features.Onboarding.UpdateOnboardingChecklist;
using IT_Form.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows
{
    /// <summary>
    /// Carries data needed to execute the SubmitOnboardingManageAccessFlow command.
    /// </summary>
    public sealed record SubmitOnboardingManageAccessFlowCommand(UpdateManageAccessCommand Command) : IRequest<ProcessResult<object>>;
    /// <summary>
    /// Carries data needed to execute the SubmitOnboardingChecklistFlow command.
    /// </summary>
    public sealed record SubmitOnboardingChecklistFlowCommand(int Id, bool CheckPhysicalClean, bool CheckO365Setup, bool CheckWindowsLogin, bool CheckEmailSetup, bool CheckSpecificSoftware, bool CheckGeneralSoftware, bool CheckAbcGroup, bool CheckEmailMailGroup, bool CheckDomainJoin, bool CheckSharedDrive, bool CheckWiFiSetup, bool CheckPrinterSetup, bool CheckScanFolder, bool CheckVPNSetup, string? ChecklistRemarks) : IRequest<ProcessResult<object>>;
    /// <summary>
    /// Carries data needed to execute the SubmitDeleteOnboardingFlow command.
    /// </summary>
    public sealed record SubmitDeleteOnboardingFlowCommand(int Id, bool IsAjax) : IRequest<ProcessResult<object>>;
    /// <summary>
    /// Carries data needed to execute the SubmitEditOnboardingFlow command.
    /// </summary>
    public sealed record SubmitEditOnboardingFlowCommand(int Id, OnboardingRequest Model, string ActionType, List<int> SelectedStandardSoftwareIds, List<int> SelectedEmailGroupIds, List<int> SelectedSharedMailboxIds) : IRequest<ProcessResult<EditOnboardingPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the SubmitSignOffHrFlow command.
    /// </summary>
    public sealed record SubmitSignOffHrFlowCommand(int Id, string SignatureBase64) : IRequest<ProcessResult<object>>;

    /// <summary>
    /// Handles the SubmitOnboardingManageAccessFlowCommand command workflow.
    /// </summary>
    public sealed class SubmitOnboardingManageAccessFlowCommandHandler : IRequestHandler<SubmitOnboardingManageAccessFlowCommand, ProcessResult<object>>
    {
        private readonly ISender _sender;
        public SubmitOnboardingManageAccessFlowCommandHandler(ISender sender) => _sender = sender;
        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(SubmitOnboardingManageAccessFlowCommand request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(request.Command, cancellationToken);
            if (result.Status == UpdateManageAccessStatus.NotFound) return ProcessResult<object>.NotFound();
            if (result.Status == UpdateManageAccessStatus.InvalidState) return ProcessResult<object>.Failure(result.Message, "Details", request.Command.OnboardingId);
            return ProcessResult<object>.Success(message: result.Message, redirectAction: "Details", requestId: result.RequestId);
        }
    }

    /// <summary>
    /// Handles the SubmitOnboardingChecklistFlowCommand command workflow.
    /// </summary>
    public sealed class SubmitOnboardingChecklistFlowCommandHandler : IRequestHandler<SubmitOnboardingChecklistFlowCommand, ProcessResult<object>>
    {
        private readonly ISender _sender;
        public SubmitOnboardingChecklistFlowCommandHandler(ISender sender) => _sender = sender;
        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(SubmitOnboardingChecklistFlowCommand request, CancellationToken cancellationToken)
            => await _sender.Send(new UpdateOnboardingChecklistCommand(request.Id, request.CheckPhysicalClean, request.CheckO365Setup, request.CheckWindowsLogin, request.CheckEmailSetup, request.CheckSpecificSoftware, request.CheckGeneralSoftware, request.CheckAbcGroup, request.CheckEmailMailGroup, request.CheckDomainJoin, request.CheckSharedDrive, request.CheckWiFiSetup, request.CheckPrinterSetup, request.CheckScanFolder, request.CheckVPNSetup, request.ChecklistRemarks), cancellationToken);
    }

    /// <summary>
    /// Handles the SubmitDeleteOnboardingFlowCommand command workflow.
    /// </summary>
    public sealed class SubmitDeleteOnboardingFlowCommandHandler : IRequestHandler<SubmitDeleteOnboardingFlowCommand, ProcessResult<object>>
    {
        private readonly ISender _sender;
        public SubmitDeleteOnboardingFlowCommandHandler(ISender sender) => _sender = sender;
        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(SubmitDeleteOnboardingFlowCommand request, CancellationToken cancellationToken)
            => await _sender.Send(new DeleteOnboardingRequestCommand(request.Id, request.IsAjax), cancellationToken);
    }

    /// <summary>
    /// Handles the SubmitEditOnboardingFlowCommand command workflow.
    /// </summary>
    public sealed class SubmitEditOnboardingFlowCommandHandler : IRequestHandler<SubmitEditOnboardingFlowCommand, ProcessResult<EditOnboardingPageViewModel>>
    {
        private readonly ISender _sender;
        public SubmitEditOnboardingFlowCommandHandler(ISender sender) => _sender = sender;
        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<EditOnboardingPageViewModel>> Handle(SubmitEditOnboardingFlowCommand request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new EditOnboardingRequestCommand(request.Id, request.Model, request.SelectedStandardSoftwareIds, request.SelectedEmailGroupIds, request.SelectedSharedMailboxIds, request.ActionType), cancellationToken);
            if (result.Status == EditOnboardingRequestStatus.NotFound) return ProcessResult<EditOnboardingPageViewModel>.NotFound();
            if (result.Status == EditOnboardingRequestStatus.Locked) return ProcessResult<EditOnboardingPageViewModel>.Failure(result.Message, "Details", request.Id);
            if (result.Status == EditOnboardingRequestStatus.Forbidden) return ProcessResult<EditOnboardingPageViewModel>.Forbidden();
            return new ProcessResult<EditOnboardingPageViewModel> { IsSuccess = true, Message = result.Message, RedirectAction = "Details", RequestId = result.RequestId, AutoOpenEmail = result.ShouldAutoOpenEmail ? $"ComposeEmail:{result.RequestId}" : null };
        }
    }

    /// <summary>
    /// Handles the SubmitSignOffHrFlowCommand command workflow.
    /// </summary>
    public sealed class SubmitSignOffHrFlowCommandHandler : IRequestHandler<SubmitSignOffHrFlowCommand, ProcessResult<object>>
    {
        private readonly ISender _sender;
        public SubmitSignOffHrFlowCommandHandler(ISender sender) => _sender = sender;
        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(SubmitSignOffHrFlowCommand request, CancellationToken cancellationToken)
            => await _sender.Send(new SignOffHrOnboardingCommand(request.Id, request.SignatureBase64), cancellationToken);
    }

    /// <summary>
    /// Validates input for the SubmitOnboardingManageAccessFlowCommand request.
    /// </summary>
    public sealed class SubmitOnboardingManageAccessFlowCommandValidator : AbstractValidator<SubmitOnboardingManageAccessFlowCommand> { public SubmitOnboardingManageAccessFlowCommandValidator() => RuleFor(x => x.Command).NotNull(); }
    /// <summary>
    /// Validates input for the SubmitOnboardingChecklistFlowCommand request.
    /// </summary>
    public sealed class SubmitOnboardingChecklistFlowCommandValidator : AbstractValidator<SubmitOnboardingChecklistFlowCommand> { public SubmitOnboardingChecklistFlowCommandValidator() => RuleFor(x => x.Id).GreaterThan(0); }
    /// <summary>
    /// Validates input for the SubmitDeleteOnboardingFlowCommand request.
    /// </summary>
    public sealed class SubmitDeleteOnboardingFlowCommandValidator : AbstractValidator<SubmitDeleteOnboardingFlowCommand> { public SubmitDeleteOnboardingFlowCommandValidator() => RuleFor(x => x.Id).GreaterThan(0); }
    /// <summary>
    /// Validates input for the SubmitEditOnboardingFlowCommand request.
    /// </summary>
    public sealed class SubmitEditOnboardingFlowCommandValidator : AbstractValidator<SubmitEditOnboardingFlowCommand> { public SubmitEditOnboardingFlowCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Model).NotNull(); RuleFor(x => x.ActionType).NotEmpty(); } }
    /// <summary>
    /// Validates input for the SubmitSignOffHrFlowCommand request.
    /// </summary>
    public sealed class SubmitSignOffHrFlowCommandValidator : AbstractValidator<SubmitSignOffHrFlowCommand> { public SubmitSignOffHrFlowCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.SignatureBase64).NotEmpty(); } }
}
