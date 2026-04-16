using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Features.Onboarding.Flows;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class OnboardingStep2QueryHandlerTests
{
    [Fact]
    public async Task Handle_LoadsDefaultRequestCategories_AndKeepsCustomRows()
    {
        var sessionModel = new OnboardingViewModel
        {
            Branch = "HQ",
            IssuedAssets =
            [
                new AssetItemViewModel
                {
                    AssetType = AssetType.Others,
                    Description = "Tablet Stand",
                    IsChecked = true
                }
            ]
        };

        var sessionAccessor = new Mock<IOnboardingSessionAccessor>();
        sessionAccessor.Setup(x => x.Get()).Returns(sessionModel);
        sessionAccessor.Setup(x => x.PrepareStep2(It.IsAny<OnboardingViewModel>()))
            .Returns<OnboardingViewModel>(model =>
            {
                model.CurrentStep = 2;
                return model;
            });

        var handler = new GetOnboardingStep2PageQueryHandler(sessionAccessor.Object);

        var result = await handler.Handle(new GetOnboardingStep2PageQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<OnboardingViewModel>(result.Data);
        Assert.Equal(
            [
                "Laptop",
                "Power Adaptor",
                "Laptop Bag",
                "Wireless Mouse",
                "Wired Mouse",
                "Desktop PC",
                "Monitor",
                "USB",
                "USB LAN Adaptor",
                "Keyboard",
                "Headset",
                "Docking Station",
                "Network Cable",
                "USB Drive",
                "Webcam"
            ],
            model.AssetCategoryOptions.Select(option => option.DisplayName).ToArray());
        Assert.Equal(16, model.IssuedAssets.Count);
        Assert.Equal("Tablet Stand", model.IssuedAssets[^1].Description);
        Assert.Equal(AssetType.Others, model.IssuedAssets[^1].AssetType);
        Assert.Null(model.IssuedAssets[^1].OtherAssetType);
    }

    [Fact]
    public async Task Handle_PreservesSelectedCategory_WhenItIsNotPartOfTheDefaultList()
    {
        var sessionModel = new OnboardingViewModel
        {
            Branch = "HQ",
            IssuedAssets =
            [
                new AssetItemViewModel
                {
                    AssetType = AssetType.Others,
                    OtherAssetType = "Mouse Pad",
                    IsChecked = true
                }
            ]
        };

        var sessionAccessor = new Mock<IOnboardingSessionAccessor>();
        sessionAccessor.Setup(x => x.Get()).Returns(sessionModel);
        sessionAccessor.Setup(x => x.PrepareStep2(It.IsAny<OnboardingViewModel>()))
            .Returns<OnboardingViewModel>(model =>
            {
                model.CurrentStep = 2;
                return model;
            });

        var handler = new GetOnboardingStep2PageQueryHandler(sessionAccessor.Object);

        var result = await handler.Handle(new GetOnboardingStep2PageQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<OnboardingViewModel>(result.Data);
        Assert.Equal(
            [
                "Laptop",
                "Power Adaptor",
                "Laptop Bag",
                "Wireless Mouse",
                "Wired Mouse",
                "Desktop PC",
                "Monitor",
                "USB",
                "USB LAN Adaptor",
                "Keyboard",
                "Headset",
                "Docking Station",
                "Network Cable",
                "USB Drive",
                "Webcam",
                "Mouse Pad"
            ],
            model.AssetCategoryOptions.Select(option => option.DisplayName).ToArray());
        Assert.Contains(model.IssuedAssets, asset =>
            asset.AssetType == AssetType.Others &&
            asset.OtherAssetType == "Mouse Pad" &&
            asset.IsChecked);
    }
}
