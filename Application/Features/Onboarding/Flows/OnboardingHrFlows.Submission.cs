// Project comment: OnboardingHrFlows.Submission.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2;
using IT_Form.Helpers;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows
{
    /// <summary>
    /// Requests the HR step 2 onboarding page model from the current onboarding session.
    /// </summary>
    public sealed record GetOnboardingStep2PageQuery() : IRequest<ProcessResult<OnboardingViewModel>>;

    /// <summary>
    /// Submits the HR step 2 onboarding form and completes the HR portion of the onboarding request.
    /// </summary>
    public sealed record SubmitOnboardingStep2FlowCommand(OnboardingViewModel Model) : IRequest<ProcessResult<object>>;

    /// <summary>
    /// Prepares the HR step 2 page by loading session data and rebuilding asset category options.
    /// </summary>
    public sealed class GetOnboardingStep2PageQueryHandler : IRequestHandler<GetOnboardingStep2PageQuery, ProcessResult<OnboardingViewModel>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;

        /// <summary>
        /// Creates the page query handler with access to the onboarding session.
        /// </summary>
        public GetOnboardingStep2PageQueryHandler(IOnboardingSessionAccessor sessionAccessor)
        {
            _sessionAccessor = sessionAccessor;
        }

        /// <summary>
        /// Returns a prepared step 2 view model with normalized asset rows for the UI.
        /// </summary>
        public Task<ProcessResult<OnboardingViewModel>> Handle(GetOnboardingStep2PageQuery request, CancellationToken cancellationToken)
        {
            var model = _sessionAccessor.PrepareStep2(_sessionAccessor.Get());
            PopulateAssetCategoryOptions(model);
            return Task.FromResult(ProcessResult<OnboardingViewModel>.Success(model));
        }

        /// <summary>
        /// Builds the selectable asset categories and aligns existing issued asset rows to those categories.
        /// </summary>
        private static void PopulateAssetCategoryOptions(OnboardingViewModel model)
        {
            var existingAssets = model.IssuedAssets ?? new List<AssetItemViewModel>();
            var defaultCategories = AssetCategoryHelper.GetDefaultOnboardingRequestCategories();

            // Start with the configured default categories so the form always has the expected asset rows.
            model.AssetCategoryOptions = defaultCategories
                .OrderBy(asset => AssetCategoryHelper.GetSortOrder(asset.AssetType, asset.OtherAssetType))
                .ThenBy(asset => AssetCategoryHelper.GetCategoryName(asset.AssetType, asset.OtherAssetType))
                .Select(asset => new OnboardingAssetCategoryOptionViewModel
                {
                    AssetType = asset.AssetType,
                    OtherAssetType = asset.OtherAssetType,
                    DisplayName = AssetCategoryHelper.GetCategoryName(asset.AssetType, asset.OtherAssetType)
                })
                .ToList();

            // Preserve non-default categories already entered in the session, such as custom "Others" assets.
            var existingCategoryOptions = existingAssets
                .Where(asset =>
                    (asset.AssetType != AssetType.Others || !string.IsNullOrWhiteSpace(asset.OtherAssetType)) &&
                    !AssetCategoryHelper.IsDefaultOnboardingRequestCategory(asset.AssetType, asset.OtherAssetType))
                .GroupBy(asset => AssetCategoryHelper.GetCategoryKey(asset.AssetType, asset.OtherAssetType))
                .Select(group => group.First())
                .OrderBy(asset => AssetCategoryHelper.GetSortOrder(asset.AssetType, asset.OtherAssetType))
                .ThenBy(asset => AssetCategoryHelper.GetCategoryName(asset.AssetType, asset.OtherAssetType))
                .Select(asset => new OnboardingAssetCategoryOptionViewModel
                {
                    AssetType = asset.AssetType,
                    OtherAssetType = asset.OtherAssetType,
                    DisplayName = AssetCategoryHelper.GetCategoryName(asset.AssetType, asset.OtherAssetType)
                });

            model.AssetCategoryOptions.AddRange(existingCategoryOptions);

            var normalizedAssets = new List<AssetItemViewModel>();
            // Ensure each category option has exactly one matching asset row for predictable model binding.
            foreach (var option in model.AssetCategoryOptions)
            {
                var existingAsset = existingAssets.FirstOrDefault(asset =>
                    AssetCategoryHelper.MatchesCategory(
                        asset.AssetType,
                        asset.OtherAssetType,
                        option.AssetType,
                        option.OtherAssetType));

                normalizedAssets.Add(existingAsset ?? new AssetItemViewModel
                {
                    AssetType = option.AssetType,
                    OtherAssetType = option.OtherAssetType,
                    Qty = 1
                });
            }

            // Keep incomplete custom rows so the UI can show validation errors instead of silently dropping input.
            normalizedAssets.AddRange(existingAssets.Where(asset =>
                asset.AssetType == AssetType.Others && string.IsNullOrWhiteSpace(asset.OtherAssetType)));

            model.IssuedAssets = normalizedAssets;
        }
    }

    /// <summary>
    /// Handles HR step 2 form submission by delegating business rules to the complete-step command.
    /// </summary>
    public sealed class SubmitOnboardingStep2FlowCommandHandler : IRequestHandler<SubmitOnboardingStep2FlowCommand, ProcessResult<object>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;
        private readonly ISender _sender;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Creates the submission handler with session, mediator, and current-user access.
        /// </summary>
        public SubmitOnboardingStep2FlowCommandHandler(IOnboardingSessionAccessor sessionAccessor, ISender sender, ICurrentUserAccessor currentUserAccessor)
        {
            _sessionAccessor = sessionAccessor;
            _sender = sender;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Completes HR step 2 and converts domain outcomes into controller-friendly process results.
        /// </summary>
        public async Task<ProcessResult<object>> Handle(SubmitOnboardingStep2FlowCommand request, CancellationToken cancellationToken)
        {
            var session = _sessionAccessor.Get();
            var result = await _sender.Send(new CompleteOnboardingHrStep2Command(request.Model, session, _currentUserAccessor.UserName), cancellationToken);

            // Map command statuses to redirects or HTTP-style outcomes expected by the flow layer.
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

    /// <summary>
    /// Validates that the HR step 2 submission includes a posted view model.
    /// </summary>
    public sealed class SubmitOnboardingStep2FlowCommandValidator : AbstractValidator<SubmitOnboardingStep2FlowCommand>
    {
        public SubmitOnboardingStep2FlowCommandValidator() => RuleFor(x => x.Model).NotNull();
    }
}
