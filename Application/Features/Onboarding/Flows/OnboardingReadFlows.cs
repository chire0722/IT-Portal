// Project comment: OnboardingReadFlows.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.GetManageAccess;
using IT_Form.Application.Features.Onboarding.GetOnboardingDelete;
using IT_Form.Application.Features.Onboarding.GetOnboardingDetails;
using IT_Form.Application.Features.Onboarding.GetOnboardingEdit;
using IT_Form.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingDetailsPage query.
    /// </summary>
    public sealed record GetOnboardingDetailsPageQuery(int Id) : IRequest<ProcessResult<OnboardingDetailsPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetManageAccessPage query.
    /// </summary>
    public sealed record GetManageAccessPageQuery(int Id) : IRequest<ProcessResult<ManageAccessViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetOnboardingEditPage query.
    /// </summary>
    public sealed record GetOnboardingEditPageQuery(int Id) : IRequest<ProcessResult<EditOnboardingPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the GetOnboardingDeletePage query.
    /// </summary>
    public sealed record GetOnboardingDeletePageQuery(int Id) : IRequest<ProcessResult<OnboardingRequest>>;

    /// <summary>
    /// Handles the GetOnboardingDetailsPageQuery query workflow.
    /// </summary>
    public sealed class GetOnboardingDetailsPageQueryHandler
        : IRequestHandler<GetOnboardingDetailsPageQuery, ProcessResult<OnboardingDetailsPageViewModel>>
    {
        private readonly ISender _sender;

        public GetOnboardingDetailsPageQueryHandler(ISender sender) => _sender = sender;

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<OnboardingDetailsPageViewModel>> Handle(GetOnboardingDetailsPageQuery request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetOnboardingDetailsQuery(request.Id), cancellationToken);
            return result.Status switch
            {
                GetOnboardingDetailsStatus.NotFound => ProcessResult<OnboardingDetailsPageViewModel>.NotFound(),
                GetOnboardingDetailsStatus.Forbidden => ProcessResult<OnboardingDetailsPageViewModel>.Forbidden(),
                _ => ProcessResult<OnboardingDetailsPageViewModel>.Success(result.ViewModel!)
            };
        }
    }

    /// <summary>
    /// Handles the GetManageAccessPageQuery query workflow.
    /// </summary>
    public sealed class GetManageAccessPageQueryHandler
        : IRequestHandler<GetManageAccessPageQuery, ProcessResult<ManageAccessViewModel>>
    {
        private readonly ISender _sender;

        public GetManageAccessPageQueryHandler(ISender sender) => _sender = sender;

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<ManageAccessViewModel>> Handle(GetManageAccessPageQuery request, CancellationToken cancellationToken)
        {
            var model = await _sender.Send(new GetManageAccessQuery(request.Id), cancellationToken);
            return model != null
                ? ProcessResult<ManageAccessViewModel>.Success(model)
                : ProcessResult<ManageAccessViewModel>.Failure(
                    "Access assignment is only available for current employees with completed onboarding records.",
                    "Details",
                    request.Id);
        }
    }

    /// <summary>
    /// Handles the GetOnboardingEditPageQuery query workflow.
    /// </summary>
    public sealed class GetOnboardingEditPageQueryHandler
        : IRequestHandler<GetOnboardingEditPageQuery, ProcessResult<EditOnboardingPageViewModel>>
    {
        private readonly ISender _sender;
        private readonly IOnboardingSessionAccessor _sessionAccessor;

        public GetOnboardingEditPageQueryHandler(ISender sender, IOnboardingSessionAccessor sessionAccessor)
        {
            _sender = sender;
            _sessionAccessor = sessionAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<EditOnboardingPageViewModel>> Handle(GetOnboardingEditPageQuery request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetOnboardingEditQuery(request.Id), cancellationToken);
            if (result.IsNotFound)
                return ProcessResult<EditOnboardingPageViewModel>.NotFound();
            if (result.IsForbidden)
                return ProcessResult<EditOnboardingPageViewModel>.Forbidden();
            if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.RedirectAction))
                return ProcessResult<EditOnboardingPageViewModel>.Failure(result.Message ?? "Request is locked.", result.RedirectAction, request.Id);
            if (result.Data != null)
                return result;

            var session = await _sessionAccessor.RestoreDraftAsync(request.Id, cancellationToken);
            if (session == null)
                return ProcessResult<EditOnboardingPageViewModel>.NotFound();

            _sessionAccessor.Save(session);
            return ProcessResult<EditOnboardingPageViewModel>.Success(redirectAction: "Step1", requestId: request.Id);
        }
    }

    /// <summary>
    /// Handles the GetOnboardingDeletePageQuery query workflow.
    /// </summary>
    public sealed class GetOnboardingDeletePageQueryHandler
        : IRequestHandler<GetOnboardingDeletePageQuery, ProcessResult<OnboardingRequest>>
    {
        private readonly ISender _sender;

        public GetOnboardingDeletePageQueryHandler(ISender sender) => _sender = sender;

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<OnboardingRequest>> Handle(GetOnboardingDeletePageQuery request, CancellationToken cancellationToken)
            => await _sender.Send(new GetOnboardingDeleteQuery(request.Id), cancellationToken);
    }
}
