// Project comment: ITAssetCommandValidationTests.cs is part of the IT Form application source code.

using System.Threading.Tasks;
using IT_Form.Application.Features.ITAsset.CreateItAsset;
using IT_Form.Application.Features.ITAsset.EditItAsset;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Tests.Helpers;
using Xunit;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the ITAssetCommandValidationTests class used by the IT Form application.
    /// </summary>
    public class ITAssetCommandValidationTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Creates data for the CreateValidator_Fails_WhenAssetTagAlreadyExists operation.
        /// </summary>
        public async Task CreateValidator_Fails_WhenAssetTagAlreadyExists()
        {
            using var context = CreateContext();
            context.ITAssets.Add(new ITAsset { AssetTag = "TAG-001", Status = AssetStatus.Available });
            await context.SaveChangesAsync();

            var validator = new CreateItAssetCommandValidator(context);
            var command = new CreateItAssetCommand(
                new ITAsset { AssetTag = "TAG-001", Status = AssetStatus.Available },
                null,
                null);

            var result = await validator.ValidateAsync(command);

            Assert.Contains(result.Errors, error => error.PropertyName == "AssetTag");
        }

        [Fact]
        /// <summary>
        /// Runs the EditValidator_Fails_WhenIssuedAssetHasNoAssignedEmployee operation.
        /// </summary>
        public async Task EditValidator_Fails_WhenIssuedAssetHasNoAssignedEmployee()
        {
            using var context = CreateContext();
            context.ITAssets.Add(new ITAsset { Id = 1, AssetTag = "TAG-001", Status = AssetStatus.Available });
            await context.SaveChangesAsync();

            var validator = new EditItAssetCommandValidator(context);
            var command = new EditItAssetCommand(
                1,
                new ITAsset { Id = 1, AssetTag = "TAG-002", Status = AssetStatus.Issued },
                null,
                null);

            var result = await validator.ValidateAsync(command);

            Assert.Contains(result.Errors, error => error.PropertyName == "CustomEmployeeName");
        }
    }
}
