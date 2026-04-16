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
    public sealed record GetOnboardingStep2PageQuery() : IRequest<ProcessResult<OnboardingViewModel>>;
    public sealed record SubmitOnboardingStep2FlowCommand(OnboardingViewModel Model) : IRequest<ProcessResult<object>>;

    public sealed class GetOnboardingStep2PageQueryHandler : IRequestHandler<GetOnboardingStep2PageQuery, ProcessResult<OnboardingViewModel>>
    {
        private readonly IOnboardingSessionAccessor _sessionAccessor;

        public GetOnboardingStep2PageQueryHandler(IOnboardingSessionAccessor sessionAccessor)
        {
            _sessionAccessor = sessionAccessor;
        }

        public Task<ProcessResult<OnboardingViewModel>> Handle(GetOnboardingStep2PageQuery request, CancellationToken cancellationToken)
        {
            var model = _sessionAccessor.PrepareStep2(_sessionAccessor.Get());
            PopulateAssetCategoryOptions(model);
            return Task.FromResult(ProcessResult<OnboardingViewModel>.Success(model));
        }

        private static void PopulateAssetCategoryOptions(OnboardingViewModel model)
        {
            var existingAssets = model.IssuedAssets ?? new List<AssetItemViewModel>();
            var defaultCategories = AssetCategoryHelper.GetDefaultOnboardingRequestCategories();
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

            var normalizedAssets = new List<AssetItemViewModel>();
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

            normalizedAssets.AddRange(existingAssets.Where(asset =>
                (asset.AssetType == AssetType.Others && string.IsNullOrWhiteSpace(asset.OtherAssetType)) ||
                !AssetCategoryHelper.IsDefaultOnboardingRequestCategory(asset.AssetType, asset.OtherAssetType))
                .Select(asset =>
                {
                    if (asset.AssetType == AssetType.Others && string.IsNullOrWhiteSpace(asset.OtherAssetType))
                    {
                        return asset;
                    }

                    return new AssetItemViewModel
                    {
                        AssetType = AssetType.Others,
                        OtherAssetType = null,
                        Description = string.IsNullOrWhiteSpace(asset.Description)
                            ? AssetCategoryHelper.GetCategoryName(asset.AssetType, asset.OtherAssetType)
                            : asset.Description,
                        Qty = asset.Qty ?? 1,
                        IsChecked = asset.IsChecked
                    };
                }));

            model.IssuedAssets = normalizedAssets;
        }
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
