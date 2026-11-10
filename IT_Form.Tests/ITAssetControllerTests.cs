// Project comment: ITAssetControllerTests.cs is part of the IT Form application source code.

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
    /// <summary>
    /// Defines the ITAssetControllerTests class used by the IT Form application.
    /// </summary>
    public class ITAssetControllerTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Runs the Index_ReturnsView_ForITRole operation.
        /// </summary>
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.ITAssets.Add(new ITAsset { AssetTag = "TAG-001", Company = "HQ", Status = AssetStatus.Available });
            await context.SaveChangesAsync();

            var controller = GetController<ITAssetController>(context, role: "IT", branch: "HQ");
            var result = await controller.Index(string.Empty, null, null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<AssetAnalyticsViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.Assets);
        }

        [Fact]
        /// <summary>
        /// Runs the Index_RestrictsAssetsToAssignedCompany_ForCompanyScopedItUser operation.
        /// </summary>
        public async Task Index_RestrictsAssetsToAssignedCompany_ForCompanyScopedItUser()
        {
            using var context = CreateContext();
            context.Companies.AddRange(
                new Company { Id = 1, Name = "Alpha", IsActive = true },
                new Company { Id = 2, Name = "Beta", IsActive = true });
            context.ITAssets.AddRange(
                new ITAsset { AssetTag = "TAG-001", Company = "Alpha", Status = AssetStatus.Available },
                new ITAsset { AssetTag = "TAG-002", Company = "Beta", Status = AssetStatus.Available });
            await context.SaveChangesAsync();

            var controller = GetController<ITAssetController>(context, role: "IT", branch: "Alpha");
            var result = await controller.Index(string.Empty, null, null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<AssetAnalyticsViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.Assets);
            Assert.Contains(model.Assets, asset => asset.Company == "Alpha");
            Assert.DoesNotContain(model.Assets, asset => asset.Company == "Beta");
        }

        [Fact]
        /// <summary>
        /// Runs the Index_FiltersAssetsBySelectedCompany operation.
        /// </summary>
        public async Task Index_FiltersAssetsBySelectedCompany()
        {
            using var context = CreateContext();
            context.Companies.AddRange(
                new Company { Id = 1, Name = "Alpha", IsActive = true },
                new Company { Id = 2, Name = "Beta", IsActive = true });
            context.ITAssets.AddRange(
                new ITAsset { AssetTag = "TAG-101", Company = "Alpha", Status = AssetStatus.Available },
                new ITAsset { AssetTag = "TAG-102", Company = "Beta", Status = AssetStatus.Available });
            await context.SaveChangesAsync();

            var controller = GetController<ITAssetController>(context, role: "SuperAdmin");
            var result = await controller.Index(string.Empty, null, null, 1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<AssetAnalyticsViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.Assets);
            Assert.Equal(1, model.SelectedCompanyId);
            Assert.Contains(model.Assets, asset => asset.Company == "Alpha");
        }

        [Fact]
        /// <summary>
        /// Runs the Edit_ForbidsCompanyScopedItUser_FromOpeningAnotherCompanyAsset operation.
        /// </summary>
        public async Task Edit_ForbidsCompanyScopedItUser_FromOpeningAnotherCompanyAsset()
        {
            using var context = CreateContext();
            var asset = new ITAsset { AssetTag = "TAG-010", Company = "Beta", Status = AssetStatus.Available };
            context.ITAssets.Add(asset);
            await context.SaveChangesAsync();

            var controller = GetController<ITAssetController>(context, role: "IT", branch: "Alpha");
            var result = await controller.Edit(asset.Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the Details_ReturnsNotFound_ForInvalidId_Adversarial operation.
        /// </summary>
        public async Task Details_ReturnsNotFound_ForInvalidId_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<ITAssetController>(context);

            var result = await controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        /// <summary>
        /// Creates data for the Create_Post_FailsIfModelIsNull_Adversarial operation.
        /// </summary>
        public async Task Create_Post_FailsIfModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<ITAssetController>(context);

            var result = await controller.Create(null!, null, null);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the Edit_Post_GuardAgainstTampering_Adversarial operation.
        /// </summary>
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
