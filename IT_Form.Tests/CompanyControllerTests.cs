// Project comment: CompanyControllerTests.cs is part of the IT Form application source code.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Company;
using CreateCompanyWriteCommand = IT_Form.Application.Features.Company.Writes.CreateCompany.CreateCompanyCommand;
using IT_Form.Controllers;
using IT_Form.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the CompanyControllerTests class used by the IT Form application.
    /// </summary>
    public class CompanyControllerTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Runs the Index_ReturnsView_ForITRole operation.
        /// </summary>
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.Companies.Add(new Company { Name = "BranchA", IsActive = true });
            await context.SaveChangesAsync();

            var controller = GetController<CompanyController>(context, role: "IT");
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<CompanyIndexListItem>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        /// <summary>
        /// Creates data for the Create_Post_ReturnsView_WhenModelIsNull_Adversarial operation.
        /// </summary>
        public async Task Create_Post_ReturnsView_WhenModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<CompanyController>(context);

            var result = await controller.Create((CompanyEditorPageViewModel)null!);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the Edit_Post_GuardAgainstTampering_Adversarial operation.
        /// </summary>
        public async Task Edit_Post_GuardAgainstTampering_Adversarial()
        {
            using var context = CreateContext();
            var company = new Company { Name = "Original", IsActive = true };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var controller = GetController<CompanyController>(context);

            var result = await controller.Edit(company.Id + 1, new CompanyEditorPageViewModel
            {
                Id = company.Id,
                Name = company.Name,
                Lot = company.Lot,
                LogoPath = company.LogoPath,
                IsLoginPageLogo = company.IsLoginPageLogo,
                IsActive = company.IsActive
            });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        /// <summary>
        /// Creates data for the Create_Post_ShowsValidationFailureMessage_WhenCreateFails operation.
        /// </summary>
        public async Task Create_Post_ShowsValidationFailureMessage_WhenCreateFails()
        {
            using var context = CreateContext();
            var sender = new Mock<ISender>();
            var model = new CompanyEditorPageViewModel
            {
                Name = "BranchA",
                IsActive = true,
                BusinessStartTime = new TimeSpan(8, 30, 0),
                BusinessEndTime = new TimeSpan(17, 30, 0)
            };

            sender.Setup(s => s.Send(It.IsAny<CreateCompanyWriteCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ValidationException(new[]
                {
                    new ValidationFailure(nameof(CompanyEditorPageViewModel.LogoPath), "Logo is required.")
                }));
            sender.Setup(s => s.Send(It.IsAny<GetCompanyCreatePageQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult<CompanyEditorPageViewModel>.Success(model));

            var controller = new CompanyController(sender.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            var result = await controller.Create(model);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(string.Empty));
            Assert.Contains(controller.ModelState[string.Empty]!.Errors, error => error.ErrorMessage == "Logo is required.");
            Assert.Equal("Logo is required.", controller.TempData["ErrorMessage"]);
        }
    }
}
