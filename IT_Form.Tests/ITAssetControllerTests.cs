using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Models.ViewModels;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    public class ITAssetControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.ITAssets.Add(new ITAsset { AssetTag = "TAG-001", Company = "HQ", Status = AssetStatus.Available });
            await context.SaveChangesAsync();

            var controller = GetController<ITAssetController>(context, role: "IT");
            var result = await controller.Index(string.Empty, null, null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<AssetAnalyticsViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.Assets);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_ForInvalidId_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<ITAssetController>(context);

            var result = await controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Post_FailsIfModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<ITAssetController>(context);

            var result = await controller.Create(null!, null, null);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Edit_Post_GuardAgainstTampering_Adversarial()
        {
            using var context = CreateContext();
            var asset = new ITAsset { AssetTag = "TAG-001", Status = AssetStatus.Available };
            context.ITAssets.Add(asset);
            await context.SaveChangesAsync();

            var controller = GetController<ITAssetController>(context);
            
            var result = await controller.Edit(asset.Id + 1, asset, null, null);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
