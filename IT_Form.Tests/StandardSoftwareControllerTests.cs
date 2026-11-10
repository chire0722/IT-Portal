// Project comment: StandardSoftwareControllerTests.cs is part of the IT Form application source code.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.StandardSoftware;
using IT_Form.Application.Features.StandardSoftware.EditSoftwareLicense;
using IT_Form.Controllers;
using IT_Form.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the StandardSoftwareControllerTests class used by the IT Form application.
    /// </summary>
    public class StandardSoftwareControllerTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Runs the Index_ReturnsView_ForITRole operation.
        /// </summary>
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.StandardSoftware.Add(new StandardSoftware { SoftwareName = "Office 365", IsActive = true });
            await context.SaveChangesAsync();

            var controller = GetController<StandardSoftwareController>(context, role: "SuperAdmin");
            var result = await controller.Index(null, null, null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<StandardSoftwareIndexPageViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.SoftwareList);
        }

        [Fact]
        /// <summary>
        /// Gets the data required by the GetEmployees_ReturnsJsonOptions operation.
        /// </summary>
        public async Task GetEmployees_ReturnsJsonOptions()
        {
            using var context = CreateContext();
            context.OnboardingRequests.Add(new OnboardingRequest
            {
                Id = 5,
                FullName = "Alicia Tan",
                Branch = "HQ",
                Status = Models.Enums.RequestStatus.Completed
            });
            await context.SaveChangesAsync();

            var controller = GetController<StandardSoftwareController>(context, role: "SuperAdmin");

            var result = await controller.GetEmployees();

            var jsonResult = Assert.IsType<JsonResult>(result);
            var employees = Assert.IsAssignableFrom<List<StandardSoftwareEmployeeOptionDto>>(jsonResult.Value);
            Assert.Single(employees);
            Assert.Equal("Alicia Tan", employees[0].Name);
        }

        [Fact]
        /// <summary>
        /// Gets the data required by the GetKeys_ReturnsJsonPayload operation.
        /// </summary>
        public async Task GetKeys_ReturnsJsonPayload()
        {
            using var context = CreateContext();
            var software = new StandardSoftware { Id = 1, SoftwareName = "Office", IsActive = true };
            var license = new SoftwareLicense
            {
                Id = 9,
                StandardSoftwareId = 1,
                StandardSoftware = software,
                SoftwareName = "Office E3",
                ContractLimit = 10
            };
            license.Keys.Add(new SoftwareKey
            {
                Id = 12,
                KeyValue = "ABC-123",
                IsAssigned = false
            });

            context.StandardSoftware.Add(software);
            context.SoftwareLicenses.Add(license);
            await context.SaveChangesAsync();

            var controller = GetController<StandardSoftwareController>(context, role: "IT");

            var result = await controller.GetKeys(9);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var payload = Assert.IsType<StandardSoftwareKeysResponseDto>(jsonResult.Value);
            Assert.False(payload.IsSubscription);
            Assert.Single(payload.Keys);
            Assert.Equal("ABC-123", payload.Keys[0].KeyValue);
        }

        [Fact]
        /// <summary>
        /// Runs the DownloadTemplate_ReturnsFileResult_WhenQueryReturnsBinaryPayload operation.
        /// </summary>
        public async Task DownloadTemplate_ReturnsFileResult_WhenQueryReturnsBinaryPayload()
        {
            var sender = new Moq.Mock<MediatR.ISender>();
            sender.Setup(s => s.Send(It.IsAny<DownloadStandardSoftwareTemplateQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult<BinaryFilePayload>.Success(
                    new BinaryFilePayload([0x50, 0x4B], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SoftwareList_Template.xlsx")));

            var controller = new StandardSoftwareController(sender.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                controller.HttpContext,
                Moq.Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var result = await controller.DownloadTemplate();

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
            Assert.Equal("SoftwareList_Template.xlsx", fileResult.FileDownloadName);
        }

        [Fact]
        /// <summary>
        /// Runs the EditLicense_ReturnsJsonFailure_WhenSeatLimitIsInvalid operation.
        /// </summary>
        public async Task EditLicense_ReturnsJsonFailure_WhenSeatLimitIsInvalid()
        {
            var sender = new Moq.Mock<MediatR.ISender>();
            sender.Setup(s => s.Send(It.IsAny<EditSoftwareLicenseCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EditSoftwareLicenseResult(
                    EditSoftwareLicenseStatus.InvalidSeatLimit,
                    "Cannot reduce seat limit below 1 assigned seat(s)."));

            var controller = new StandardSoftwareController(sender.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };

            var result = await controller.EditLicense(1, "Office E3", null, true, false, null, 0, null, null, null);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.False(GetAnonymousProperty<bool>(jsonResult.Value, "success"));
            Assert.Equal(
                "Cannot reduce seat limit below 1 assigned seat(s).",
                GetAnonymousProperty<string>(jsonResult.Value, "message"));
        }

        // StandardSoftwareController does not have a Details action in this version

        private static T GetAnonymousProperty<T>(object? source, string propertyName)
        {
            Assert.NotNull(source);
            var property = source!.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property);
            return Assert.IsType<T>(property!.GetValue(source));
        }
    }
}
