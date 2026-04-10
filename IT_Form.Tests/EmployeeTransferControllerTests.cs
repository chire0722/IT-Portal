using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Data;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.EmployeeTransfer.ConfirmEmployeeTransfer;
using IT_Form.Application.Features.EmployeeTransfer.GetEmployeeTransferConfirmationPage;
using IT_Form.Application.Features.EmployeeTransfer.Shared;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    public class EmployeeTransferControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task Create_Post_ReturnsView_WhenModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<EmployeeTransferController>(context);

            var result = await controller.Create(null!);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_ForInvalidId_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<EmployeeTransferController>(context);

            var result = await controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Confirm_Get_RejectsExpiredAccessToken()
        {
            using var context = CreateContext();
            var transfer = new EmployeeTransfer
            {
                FullName = "Expired Transfer",
                FromDepartment = "IT",
                ToDepartment = "HR",
                Status = RequestStatus.PendingEmployeeSign,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-8),
                AccessToken = Guid.NewGuid()
            };
            context.EmployeeTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var controller = GetController<EmployeeTransferController>(context, role: "IT");
            var result = await controller.Confirm(transfer.AccessToken);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model);
            Assert.Contains("expired", controller.TempData["ErrorMessage"]?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Confirm_Get_ReturnsTypedPageModel_ForValidToken()
        {
            using var context = CreateContext();
            var software = new StandardSoftware
            {
                Id = 1,
                SoftwareName = "Office",
                IsActive = true
            };
            var license = new SoftwareLicense
            {
                Id = 1,
                StandardSoftwareId = 1,
                StandardSoftware = software,
                SoftwareName = "Office 365"
            };
            var transfer = new EmployeeTransfer
            {
                FullName = "Valid Transfer",
                EmployeeID = "EMP-01",
                FromDepartment = "IT",
                ToDepartment = "HR",
                FromBranch = "HQ",
                ToBranch = "Branch A",
                Status = RequestStatus.PendingEmployeeSign,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AccessToken = Guid.NewGuid()
            };
            context.StandardSoftware.Add(software);
            context.SoftwareLicenses.Add(license);
            context.EmployeeTransfers.Add(transfer);
            context.IssuedSoftware.Add(new IssuedSoftware
            {
                EmployeeTransfer = transfer,
                SoftwareName = "Office 365",
                SoftwareLicenseId = 1
            });
            await context.SaveChangesAsync();

            var controller = GetController<EmployeeTransferController>(context, role: "IT");

            var result = await controller.Confirm(transfer.AccessToken);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EmployeeTransferConfirmationPageViewModel>(viewResult.Model);
            Assert.Equal("Valid Transfer", model.FullName);
            Assert.Single(model.IssuedSoftware);
        }

        [Fact]
        public async Task Confirm_Post_CompletesTransfer_AndRotatesAccessToken()
        {
            using var context = CreateContext();
            var originalToken = Guid.NewGuid();
            var transfer = new EmployeeTransfer
            {
                FullName = "Token Transfer",
                FromDepartment = "IT",
                ToDepartment = "Finance",
                Status = RequestStatus.PendingEmployeeSign,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AccessToken = originalToken
            };
            context.EmployeeTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var mockEnv = new Moq.Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var mockPdf = new Moq.Mock<IT_Form.Services.IPdfService>();
            var mockIntegrity = new Moq.Mock<IT_Form.Services.ISignatureIntegrityService>();
            var sender = new Moq.Mock<ISender>();
            sender.Setup(s => s.Send(It.IsAny<ConfirmEmployeeTransferCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfirmEmployeeTransferCommand cmd, CancellationToken _) =>
                {
                    var req = context.EmployeeTransfers.FirstOrDefault(t => t.AccessToken == cmd.Token);
                    if (req == null)
                    {
                        return ProcessResult<object>.NotFound();
                    }

                    req.EmployeeSignature = cmd.EmployeeSignature;
                    req.Status = RequestStatus.Completed;
                    req.AccessToken = Guid.NewGuid();
                    context.SaveChanges();
                    return ProcessResult<object>.Success(message: "ok", requestId: req.Id);
                });

            var controller = new EmployeeTransferController(sender.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                controller.HttpContext,
                Moq.Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            var result = await controller.Confirm(originalToken, "data:image/png;base64,SIG");

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("Success", view.ViewName);

            var saved = await context.EmployeeTransfers.FindAsync(transfer.Id);
            Assert.NotNull(saved);
            Assert.Equal(RequestStatus.Completed, saved!.Status);
            Assert.NotEqual(originalToken, saved.AccessToken);
            Assert.Equal("data:image/png;base64,SIG", saved.EmployeeSignature);
        }

        [Fact]
        public async Task Create_Post_ReturnsView_WhenSelectedHodCompanyDoesNotMatchEmployeeCompany()
        {
            using var context = CreateContext();

            context.OnboardingRequests.Add(new OnboardingRequest
            {
                Id = 1,
                FullName = "Employee One",
                Department = "Engineering",
                Branch = "Alpha (Lot 1)",
                Status = RequestStatus.Completed
            });

            var hodUser = new ApplicationUser
            {
                Id = "hod-1",
                UserName = "hod.user",
                Email = "hod@test.com",
                FullName = "HOD User",
                IsActive = true
            };
            context.Users.Add(hodUser);
            context.UserTitles.Add(new UserTitle { UserId = hodUser.Id, Title = "HOD" });
            context.UserCompanies.Add(new UserCompany { UserId = hodUser.Id, CompanyName = "Beta" });
            await context.SaveChangesAsync();

            var controller = GetController<EmployeeTransferController>(context, role: "HR", branch: "Alpha", username: "hr_user");

            var model = new EmployeeTransferCreatePageViewModel
            {
                OnboardingRequestId = 1,
                ToDepartment = "Finance",
                EffectiveDate = DateTime.Today,
                HODEmail = "hod@test.com"
            };

            var result = await controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.Contains(
                viewResult.ViewData.ModelState[nameof(EmployeeTransfer.HODEmail)]!.Errors,
                e => e.ErrorMessage.Contains("not assigned to the employee company", StringComparison.OrdinalIgnoreCase));
            Assert.Empty(context.EmployeeTransfers.ToList());
        }

        [Fact]
        public async Task Create_Post_ReturnsView_WhenSelectedHodDoesNotMatchTargetBranch()
        {
            using var context = CreateContext();

            context.OnboardingRequests.Add(new OnboardingRequest
            {
                Id = 2,
                FullName = "Employee Two",
                Department = "Engineering",
                Branch = "Alpha (Lot 1)",
                Status = RequestStatus.Completed
            });

            var hodUser = new ApplicationUser
            {
                Id = "hod-2",
                UserName = "hod.target",
                Email = "hodtarget@test.com",
                FullName = "Target HOD",
                IsActive = true
            };
            context.Users.Add(hodUser);
            context.UserTitles.Add(new UserTitle { UserId = hodUser.Id, Title = "HOD" });
            context.UserCompanies.Add(new UserCompany { UserId = hodUser.Id, CompanyName = "Alpha (Lot 1)" });
            await context.SaveChangesAsync();

            var controller = GetController<EmployeeTransferController>(context, role: "HR", branch: "Alpha", username: "hr_user");

            var model = new EmployeeTransferCreatePageViewModel
            {
                OnboardingRequestId = 2,
                ToDepartment = "Finance",
                ToBranch = "Beta (Lot 2)",
                EffectiveDate = DateTime.Today,
                HODEmail = "hodtarget@test.com"
            };

            var result = await controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.Contains(
                viewResult.ViewData.ModelState[nameof(EmployeeTransfer.HODEmail)]!.Errors,
                e => e.ErrorMessage.Contains("not assigned to the employee company", StringComparison.OrdinalIgnoreCase));
            Assert.Empty(context.EmployeeTransfers.ToList());
        }

        // OffboardingController uses Step1/Step2 wizard instead of a standard Edit action
    }
}
