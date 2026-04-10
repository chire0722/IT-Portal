using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.EditOnboardingRequest;
using IT_Form.Application.Features.Onboarding.Flows;
using IT_Form.Application.Features.Onboarding.GetOnboardingDelete;
using IT_Form.Application.Features.Onboarding.GetOnboardingIndex;
using IT_Form.Application.Features.Onboarding.UpdateManageAccess;
using IT_Form.Controllers;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    public class OnboardingControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task Index_ReturnsView_ForHRRole()
        {
            using var context = CreateContext();
            var sender = new Mock<MediatR.ISender>();
            var requests = new PaginatedList<OnboardingIndexListItem>(
                new List<OnboardingIndexListItem> { new() { FullName = "New Hire", Status = RequestStatus.Draft } },
                1,
                1,
                10);
            sender.Setup(s => s.Send(It.IsAny<GetOnboardingIndexPageQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult<GetOnboardingIndexResult>.Success(
                    new GetOnboardingIndexResult(requests, string.Empty, null, 3)));

            var controller = BuildController(sender.Object, role: "HR");
            var result = await controller.Index(string.Empty, null, 1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<OnboardingIndexListItem>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Step1_Post_SavesToSession_ValidData()
        {
            using var context = CreateContext();
            var sender = new Mock<MediatR.ISender>();
            var session = new MockSession();
            sender.Setup(s => s.Send(It.IsAny<SubmitOnboardingStep1FlowCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SubmitOnboardingStep1FlowCommand cmd, CancellationToken _) =>
                {
                    session.SetString("OnboardingWizard", JsonConvert.SerializeObject(new OnboardingViewModel
                    {
                        FullName = cmd.Model.FullName,
                        Department = cmd.Model.Department,
                        CurrentStep = 2
                    }));

                    return ProcessResult<object>.Success(message: "Step 1 saved.", redirectAction: "Step2");
                });

            var controller = BuildController(sender.Object, session: session);
            var model = new OnboardingStep1PageViewModel { FullName = "Test User", Department = "IT" };

            var result = await controller.Step1(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Step2", redirectResult.ActionName);

            // Verify session
            var sessionData = controller.HttpContext.Session.GetString("OnboardingWizard");
            Assert.NotNull(sessionData);
            Assert.Contains("Test User", sessionData);
        }

        [Fact]
        public async Task Step1_Post_ReturnsView_WhenModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var sender = new Mock<MediatR.ISender>();
            sender.Setup(s => s.Send(It.Is<SubmitOnboardingStep1FlowCommand>(c => c.Model == null), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult<object>.Success(new ViewResult()));
            var controller = BuildController(sender.Object);

            var result = await controller.Step1(null!);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task GeneratePdf_ReturnsFileResult_WhenQueryReturnsBinaryPayload()
        {
            var sender = new Mock<MediatR.ISender>();
            sender.Setup(s => s.Send(It.Is<GenerateOnboardingPdfFileQuery>(q => q.Id == 12), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult<BinaryFilePayload>.Success(
                    new BinaryFilePayload([0x25, 0x50, 0x44, 0x46], "application/pdf", "onboarding.pdf")));

            var controller = BuildController(sender.Object, role: "HR");

            var result = await controller.GeneratePdf(12);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("onboarding.pdf", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task Edit_Post_BlockedIfPendingSign_Adversarial()
        {
            using var context = CreateContext();
            var req = new OnboardingRequest { Id = 1, FullName = "Sample", Status = RequestStatus.PendingEmployeeSign };
            context.OnboardingRequests.Add(req);
            await context.SaveChangesAsync();

            var sender = new Mock<MediatR.ISender>();
            sender.Setup(s => s.Send(It.IsAny<SubmitEditOnboardingFlowCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult<EditOnboardingPageViewModel>.Failure(
                    "This request is locked.",
                    "Details",
                    req.Id));

            var controller = BuildController(sender.Object, role: "IT");
            var result = await controller.Edit(req.Id, req, "save", new List<int>(), new List<int>(), new List<int>());

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Contains("locked", controller.TempData["ErrorMessage"]?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Delete_Blocked_ForHR_WhenNotDraft_Adversarial()
        {
            using var context = CreateContext();
            var req = new OnboardingRequest { Id = 1, Status = RequestStatus.PendingITApproval };
            context.OnboardingRequests.Add(req);
            await context.SaveChangesAsync();

            var sender = new Mock<MediatR.ISender>();
            sender.Setup(s => s.Send(It.IsAny<GetOnboardingDeletePageQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult<OnboardingRequest>.Failure(
                    "Only Draft or Rejected requests can be deleted.",
                    "Index"));

            var controller = BuildController(sender.Object, role: "HR");
            var result = await controller.Delete(req.Id);

            Assert.IsType<RedirectToActionResult>(result); 
        }

        [Fact]
        public async Task ManageAccess_Get_ReturnsCurrentEmployeeAssignments_ForIT()
        {
            using var context = CreateContext();
            var sender = new Mock<MediatR.ISender>();
            sender.Setup(s => s.Send(It.Is<GetManageAccessPageQuery>(q => q.Id == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult<ManageAccessViewModel>.Success(new ManageAccessViewModel
                {
                    OnboardingId = 1,
                    SelectedEmailGroupIds = new List<int> { 11 },
                    SelectedSharedMailboxIds = new List<int> { 21 },
                    FolderPermissions = new List<FolderPermissionViewModel>
                    {
                        new() { FolderName = "HR", CanRead = true, CanModify = false }
                    }
                }));

            context.OnboardingRequests.Add(new OnboardingRequest
            {
                Id = 1,
                FullName = "Alicia Tan",
                Status = RequestStatus.Completed,
            });
            await context.SaveChangesAsync();

            var controller = BuildController(sender.Object, role: "IT");
            var result = await controller.ManageAccess(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ManageAccessViewModel>(viewResult.Model);

            Assert.Equal(1, model.OnboardingId);
            Assert.Contains(11, model.SelectedEmailGroupIds);
            Assert.Contains(21, model.SelectedSharedMailboxIds);
            Assert.Contains(model.FolderPermissions, f => f.FolderName == "HR" && f.CanRead);
        }

        [Fact]
        public async Task ManageAccess_Post_UpdatesAssignments_ForCurrentEmployee()
        {
            using var context = CreateContext();

            context.EmailGroups.AddRange(
                new EmailGroup { Id = 10, GroupName = "Legacy Group", GroupEmail = "legacy@example.com", IsActive = true },
                new EmailGroup { Id = 12, GroupName = "Precast Group", GroupEmail = "precast@example.com", IsActive = true });

            context.SharedMailboxes.AddRange(
                new SharedMailbox { Id = 20, MailboxName = "Legacy Mailbox", MailboxAddress = "legacy.mailbox@example.com", IsActive = true },
                new SharedMailbox { Id = 22, MailboxName = "IT", MailboxAddress = "itdept@example.com", IsActive = true });

            context.ServerFolders.AddRange(
                new ServerFolder { Id = 30, FolderName = "Projects", IsActive = true },
                new ServerFolder { Id = 32, FolderName = "Operations", IsActive = true });

            context.OnboardingRequests.Add(new OnboardingRequest
            {
                Id = 2,
                FullName = "Brian Lee",
                Status = RequestStatus.Completed,
                RequestEmailGroups = new List<RequestEmailGroup>
                {
                    new RequestEmailGroup { EmailGroupId = 10, GroupName = "Legacy Group", GroupEmail = "legacy@example.com" }
                },
                RequestSharedMailboxes = new List<RequestSharedMailbox>
                {
                    new RequestSharedMailbox { SharedMailboxId = 20, MailboxName = "Legacy Mailbox", MailboxAddress = "legacy.mailbox@example.com" }
                },
                FolderPermissions = new List<FolderPermission>
                {
                    new FolderPermission { FolderName = "Projects", CanRead = true, CanModify = false }
                }
            });
            await context.SaveChangesAsync();

            var sender = new Mock<MediatR.ISender>();
            sender.Setup(s => s.Send(It.IsAny<SubmitOnboardingManageAccessFlowCommand>(), It.IsAny<CancellationToken>()))
                .Returns(async (SubmitOnboardingManageAccessFlowCommand cmd, CancellationToken ct) =>
                {
                    var updateResult = await new UpdateManageAccessCommandHandler(context, new CurrentEmployeeAccessService(context))
                        .Handle(cmd.Command, ct);

                    return ProcessResult<object>.Success(
                        message: updateResult.Message,
                        redirectAction: "Details",
                        requestId: updateResult.RequestId);
                });

            var controller = BuildController(sender.Object, role: "IT", username: "it-user");
            var model = new UpdateManageAccessCommand(
                2,
                new List<int> { 12 },
                "Manual AD security group",
                new List<int> { 22 },
                "board@example.com",
                new List<FolderPermissionViewModel>
                {
                    new FolderPermissionViewModel { FolderName = "Projects", CanRead = false, CanModify = false },
                    new FolderPermissionViewModel { FolderName = "Operations", CanRead = true, CanModify = true }
                },
                @"\\fileserver\special");

            var result = await controller.ManageAccess(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            var updated = await context.OnboardingRequests
                .Include(r => r.RequestEmailGroups)
                .Include(r => r.RequestSharedMailboxes)
                .Include(r => r.FolderPermissions)
                .FirstAsync(r => r.Id == 2);

            var updatedEmailGroup = Assert.Single(updated.RequestEmailGroups);
            Assert.Equal(12, updatedEmailGroup.EmailGroupId);
            Assert.Equal("Precast Group", updatedEmailGroup.GroupName);

            var updatedMailbox = Assert.Single(updated.RequestSharedMailboxes);
            Assert.Equal(22, updatedMailbox.SharedMailboxId);
            Assert.Equal("IT", updatedMailbox.MailboxName);

            var updatedFolder = Assert.Single(updated.FolderPermissions);
            Assert.Equal("Operations", updatedFolder.FolderName);
            Assert.True(updatedFolder.CanRead);
            Assert.True(updatedFolder.CanModify);

            Assert.True(updated.PrecastGroup);
            Assert.True(updated.MailboxIT);
            Assert.True(updated.OtherGroup);
            Assert.True(updated.HasOtherSharedMailbox);
            Assert.True(updated.HasOtherFolder);
        }

        private static OnboardingController BuildController(MediatR.ISender sender, string role = "HR", string username = "testuser", ISession? session = null)
        {
            var controller = new OnboardingController(sender);
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim("UserTitle", role),
                    new Claim("UserCompany", "HQ")
                }, "mock")),
                Session = session ?? new MockSession(),
                RequestServices = new ServiceCollection().BuildServiceProvider()
            };

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext,
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var url = new Mock<IUrlHelper>();
            url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("http://test/fake");
            controller.Url = url.Object;

            return controller;
        }
    }
}
