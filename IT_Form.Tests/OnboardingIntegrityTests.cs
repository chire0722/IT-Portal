// Project comment: OnboardingIntegrityTests.cs is part of the IT Form application source code.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.Flows;
using IT_Form.Controllers;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Newtonsoft.Json;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the OnboardingIntegrityTests class used by the IT Form application.
    /// </summary>
    public class OnboardingIntegrityTests
    {
        /// <summary>
        /// Gets the data required by the GetContext operation.
        /// </summary>
        private ApplicationDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
                
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockDbLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ApplicationDbContext>>();
            var mockLicenseEncryption = new Mock<ILicenseEncryptionService>();
            
            return new ApplicationDbContext(options, mockHttpContextAccessor.Object, mockDbLogger.Object, mockLicenseEncryption.Object);
        }

        /// <summary>
        /// Gets the data required by the GetController operation.
        /// </summary>
        private OnboardingController GetController(ApplicationDbContext context, string userTitle = "HR")
        {
            var mockWizardService = new Mock<IOnboardingWizardService>();
            mockWizardService
                .Setup(w => w.GetFromSession(It.IsAny<ISession>()))
                .Returns((ISession session) =>
                {
                    var payload = session.GetString("OnboardingWizard");
                    if (!string.IsNullOrWhiteSpace(payload))
                    {
                        var restored = JsonConvert.DeserializeObject<OnboardingViewModel>(payload);
                        if (restored != null)
                        {
                            return restored;
                        }
                    }

                    return new OnboardingViewModel();
                });
            mockWizardService
                .Setup(w => w.SaveToSession(It.IsAny<ISession>(), It.IsAny<OnboardingViewModel>()))
                .Callback((ISession session, OnboardingViewModel vm) =>
                    session.SetString("OnboardingWizard", JsonConvert.SerializeObject(vm)));
            mockWizardService
                .Setup(w => w.MapStep1ToSession(It.IsAny<OnboardingViewModel>(), It.IsAny<OnboardingViewModel>()))
                .Callback((OnboardingViewModel source, OnboardingViewModel target) =>
                {
                    target.Id = source.Id;
                    target.FullName = source.FullName;
                    target.EmployeeID = source.EmployeeID;
                    target.Designation = source.Designation;
                    target.Department = source.Department;
                    target.Branch = source.Branch;
                    target.DeskNumber = source.DeskNumber;
                    target.JoinDate = source.JoinDate;
                    target.EmailAddress = source.EmailAddress;
                    target.ContactNumber = source.ContactNumber;
                    target.HODEmail = source.HODEmail;
                });
            mockWizardService
                .Setup(w => w.MapSessionToRequest(It.IsAny<OnboardingViewModel>(), It.IsAny<OnboardingRequest>()))
                .Callback((OnboardingViewModel source, OnboardingRequest target) =>
                {
                    target.FullName = source.FullName;
                    target.EmployeeID = source.EmployeeID;
                    target.Designation = source.Designation;
                    target.Department = source.Department;
                    target.Branch = source.Branch;
                    target.DeskNumber = source.DeskNumber;
                    target.JoinDate = source.JoinDate;
                    target.EmailAddress = source.EmailAddress;
                    target.ContactNumber = source.ContactNumber;
                    target.HODEmail = source.HODEmail;
                });
            var mockSender = new Mock<MediatR.ISender>();
            OnboardingController? controller = null;

            mockSender.Setup(s => s.Send(It.IsAny<GetOnboardingEditPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync((GetOnboardingEditPageQuery cmd, System.Threading.CancellationToken _) =>
                {
                    var request = context.OnboardingRequests.Find(cmd.Id);
                    if (request == null)
                    {
                        return ProcessResult<EditOnboardingPageViewModel>.NotFound();
                    }

                    return request.Status == RequestStatus.Completed || request.Status == RequestStatus.PendingEmployeeSign
                        ? ProcessResult<EditOnboardingPageViewModel>.Failure("This request is locked.", "Details", cmd.Id)
                        : ProcessResult<EditOnboardingPageViewModel>.Success(new EditOnboardingPageViewModel());
                });
            mockSender.Setup(s => s.Send(It.IsAny<SubmitEditOnboardingFlowCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync((SubmitEditOnboardingFlowCommand cmd, System.Threading.CancellationToken _) =>
                {
                    var request = context.OnboardingRequests.Find(cmd.Id);
                    if (request == null)
                    {
                        return ProcessResult<EditOnboardingPageViewModel>.NotFound();
                    }

                    return request.Status == RequestStatus.Completed || request.Status == RequestStatus.PendingEmployeeSign
                        ? ProcessResult<EditOnboardingPageViewModel>.Failure("This request is locked.", "Details", cmd.Id)
                        : ProcessResult<EditOnboardingPageViewModel>.Success(message: "Request updated.", redirectAction: "Details", requestId: cmd.Id);
                });
            mockSender.Setup(s => s.Send(It.IsAny<SubmitOnboardingStep1FlowCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync((SubmitOnboardingStep1FlowCommand cmd, System.Threading.CancellationToken _) =>
                {
                    var payload = controller?.HttpContext.Session.GetString("OnboardingWizard");
                    var sessionModel = !string.IsNullOrWhiteSpace(payload)
                        ? JsonConvert.DeserializeObject<OnboardingViewModel>(payload)
                        : null;

                    if (sessionModel?.Id > 0)
                    {
                        var request = context.OnboardingRequests.Find(sessionModel.Id);
                        if (request?.Status == RequestStatus.PendingEmployeeSign || request?.Status == RequestStatus.Completed)
                        {
                            return ProcessResult<object>.Failure("This request is locked.", "Details", sessionModel.Id);
                        }
                    }

                    controller!.HttpContext.Session.SetString("OnboardingWizard", JsonConvert.SerializeObject(new OnboardingViewModel
                    {
                        FullName = cmd.Model.FullName,
                        CurrentStep = 2
                    }));

                    return ProcessResult<object>.Success(message: "Step 1 saved.", redirectAction: "Step2");
                });
            controller = new OnboardingController(
                mockSender.Object);
            
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("UserTitle", userTitle)
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            controller.HttpContext.RequestServices = new ServiceCollection()
                .AddSingleton(mockWizardService.Object)
                .BuildServiceProvider();

            var tempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            var url = new Mock<IUrlHelper>();
            url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("http://test/fake");
            controller.Url = url.Object;

            // Mock Session
            var mockSession = new MockSession();
            controller.HttpContext.Session = mockSession;

            return controller;
        }

        [Fact]
        /// <summary>
        /// Runs the Edit_Get_LockedIfCompleted operation.
        /// </summary>
        public async Task Edit_Get_LockedIfCompleted()
        {
            using var context = GetContext();
            var request = new OnboardingRequest { FullName = "Test", Status = RequestStatus.Completed };
            context.OnboardingRequests.Add(request);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            var result = await controller.Edit(request.Id);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Contains("locked", controller.TempData["ErrorMessage"]?.ToString() ?? "");
        }

        [Fact]
        /// <summary>
        /// Runs the Edit_Post_LockedIfCompleted operation.
        /// </summary>
        public async Task Edit_Post_LockedIfCompleted()
        {
            using var context = GetContext();
            var request = new OnboardingRequest { FullName = "Test", Status = RequestStatus.Completed };
            context.OnboardingRequests.Add(request);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            var model = new OnboardingRequest { Id = request.Id, FullName = "Modified" };
            var result = await controller.Edit(request.Id, model, "save", new List<int>(), new List<int>(), new List<int>());

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            
            // Verify no change
            var saved = await context.OnboardingRequests.FindAsync(request.Id);
            Assert.Equal("Test", saved!.FullName);
        }

        [Fact]
        /// <summary>
        /// Runs the Step1_Post_LockedIfPendingSign operation.
        /// </summary>
        public async Task Step1_Post_LockedIfPendingSign()
        {
            using var context = GetContext();
            var request = new OnboardingRequest { Id = 1, FullName = "Test", Status = RequestStatus.PendingEmployeeSign };
            context.OnboardingRequests.Add(request);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            
            // Set session
            var sessionModel = new OnboardingViewModel { Id = 1, FullName = "Old Name" };
            controller.HttpContext.Session.SetString("OnboardingWizard", JsonConvert.SerializeObject(sessionModel));

            var inputModel = new OnboardingStep1PageViewModel { FullName = "Illegal Change" };
            var finalResult = await controller.Step1(inputModel);

            var redirectResult = Assert.IsType<RedirectToActionResult>(finalResult);
            Assert.Equal("Details", redirectResult.ActionName);
        }
    }

    /// <summary>
    /// Defines the MockSession class used by the IT Form application.
    /// </summary>
    public class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionData = new();
        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _sessionData.Keys;
        /// <summary>
        /// Runs the Clear operation.
        /// </summary>
        public void Clear() => _sessionData.Clear();
        /// <summary>
        /// Runs the CommitAsync operation.
        /// </summary>
        public async Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => await Task.CompletedTask;
        /// <summary>
        /// Runs the LoadAsync operation.
        /// </summary>
        public async Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => await Task.CompletedTask;
        /// <summary>
        /// Runs the Remove operation.
        /// </summary>
        public void Remove(string key) => _sessionData.Remove(key);
        /// <summary>
        /// Runs the Set operation.
        /// </summary>
        public void Set(string key, byte[] value) => _sessionData[key] = value;
        /// <summary>
        /// Runs the TryGetValue operation.
        /// </summary>
        public bool TryGetValue(string key, out byte[] value) => _sessionData.TryGetValue(key, out value!);
    }
}

