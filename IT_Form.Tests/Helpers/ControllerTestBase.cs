// Project comment: ControllerTestBase.cs is part of the IT Form application source code.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using IT_Form.Application.Common.Mapping;
using IT_Form.Data;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Company;
using IT_Form.Application.Features.Department;
using IT_Form.Application.Features.EmailContact;
using IT_Form.Application.Features.EmailGroup;
using IT_Form.Application.Features.ITAsset.CreateItAsset;
using IT_Form.Application.Features.ITAsset.EditItAsset;
using IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup;
using IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2;
using IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest;
using IT_Form.Application.Features.Onboarding.EditOnboardingRequest;
using IT_Form.Application.Features.Onboarding.SaveOnboardingDraft;
using IT_Form.Application.Features.Onboarding.SaveOnboardingStep1;
using IT_Form.Application.Features.Onboarding.SignOffHrOnboarding;
using IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep3;
using IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4;
using IT_Form.Application.Features.Onboarding.UpdateManageAccess;
using IT_Form.Application.Features.Signature;
using IT_Form.Application.Features.Signature.AcknowledgeEmployeeSignature;
using IT_Form.Application.Features.ServerFolder;
using IT_Form.Application.Features.SharedMailbox;
using IT_Form.Application.Features.StandardSoftware;
using IT_Form.Models;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace IT_Form.Tests.Helpers
{
    /// <summary>
    /// Defines the ControllerTestBase class used by the IT Form application.
    /// </summary>
    public abstract class ControllerTestBase : IDisposable
    {
        protected readonly DbContextOptions<ApplicationDbContext> DbOptions;
        protected Mock<IHttpContextAccessor> MockHttpContextAccessor;
        protected Mock<ILicenseEncryptionService> MockEncryptionService;
        protected Mock<IPdfService> MockPdfService;
        protected Mock<ISignatureIntegrityService> MockIntegrityService;

        protected ControllerTestBase()
        {
            MapsterConfig.RegisterMappings();

            DbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            MockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            MockEncryptionService = new Mock<ILicenseEncryptionService>();
            MockPdfService = new Mock<IPdfService>();
            MockIntegrityService = new Mock<ISignatureIntegrityService>();

            // Default encryption passthrough
            MockEncryptionService.Setup(s => s.EncryptKey(It.IsAny<string>())).Returns((string s) => s);
            MockEncryptionService.Setup(s => s.DecryptKey(It.IsAny<string>())).Returns((string s) => s);
            MockIntegrityService
                .Setup(s => s.ComputeHmac(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>()))
                .Returns("HMAC");
            MockIntegrityService
                .Setup(s => s.Verify(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(true);
        }

        /// <summary>
        /// Creates data for the CreateContext operation.
        /// </summary>
        protected ApplicationDbContext CreateContext()
        {
            return new ApplicationDbContext(DbOptions, MockHttpContextAccessor.Object, new Mock<ILogger<ApplicationDbContext>>().Object, MockEncryptionService.Object);
        }

        protected T GetController<T>(ApplicationDbContext context, string role = "IT", string branch = "HQ", string username = "testuser") where T : class
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("UserTitle", role),
                new Claim("UserCompany", branch)
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest"; // Default to AJAX for many controllers
            MockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);
            
            // Mock Session
            httpContext.Session = new MockSession();
            httpContext.RequestServices = BuildRequestServices();

            var controller = (T)Activator.CreateInstance(typeof(T), GetConstructorArgs(typeof(T), context))!;

            if (controller is ControllerBase controllerBase)
            {
                controllerBase.ControllerContext = new ControllerContext { HttpContext = httpContext };
            }

            if (controller is Controller mvcController)
            {
                mvcController.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
                
                // Mock UrlHelper
                var mockUrl = new Mock<IUrlHelper>();
                mockUrl.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("http://test/fake");
                mvcController.Url = mockUrl.Object;
            }

            return controller;
        }

        /// <summary>
        /// Gets the data required by the GetConstructorArgs operation.
        /// </summary>
        private object[] GetConstructorArgs(Type controllerType, ApplicationDbContext context)
        {
            var constructor = controllerType.GetConstructors()[0];
            var parameters = constructor.GetParameters();
            var args = new List<object>();

            foreach (var p in parameters)
            {
                if (p.ParameterType == typeof(ApplicationDbContext)) args.Add(context);
                else if (p.ParameterType == typeof(IPdfService)) args.Add(MockPdfService.Object);
                else if (p.ParameterType == typeof(ISignatureIntegrityService)) args.Add(MockIntegrityService.Object);
                else if (p.ParameterType == typeof(UserManager<ApplicationUser>)) args.Add(GetMockUserManager().Object);
                else if (p.ParameterType.IsInterface && p.ParameterType.Name.StartsWith("ILogger"))
                {
                    var loggerType = typeof(Mock<>).MakeGenericType(p.ParameterType);
                    args.Add(((Mock)Activator.CreateInstance(loggerType)!).Object);
                }
                else if (p.ParameterType == typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) args.Add(new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().Object);
                else if (p.ParameterType.IsInterface)
                {
                    if (p.ParameterType == typeof(MediatR.ISender) || p.ParameterType == typeof(MediatR.IMediator))
                    {
                        var sender = new Mock<MediatR.IMediator>();
                        var scopedCurrentUser = new CurrentUserAccessor(MockHttpContextAccessor.Object);
                        sender.Setup(s => s.Send(It.IsAny<UpdateManageAccessCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((UpdateManageAccessCommand cmd, System.Threading.CancellationToken _) =>
                                new UpdateManageAccessResult(
                                    UpdateManageAccessStatus.Updated,
                                    cmd.OnboardingId,
                                    "Access assignment updated."));
                        sender.Setup(s => s.Send(It.IsAny<SignOffHrOnboardingCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((SignOffHrOnboardingCommand cmd, System.Threading.CancellationToken _) =>
                                ProcessResult<object>.Success(
                                    message: "Signature saved! The request has been submitted to IT.",
                                    redirectAction: "Details",
                                    requestId: cmd.Id));
                        sender.Setup(s => s.Send(It.IsAny<DeleteOnboardingRequestCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((DeleteOnboardingRequestCommand cmd, System.Threading.CancellationToken _) =>
                                ProcessResult<object>.Success(
                                    message: "Request deleted.",
                                    redirectAction: "Index",
                                    requestId: cmd.Id));
                        sender.Setup(s => s.Send(It.IsAny<SaveOnboardingDraftCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((SaveOnboardingDraftCommand cmd, System.Threading.CancellationToken _) =>
                                new SaveOnboardingDraftResult(
                                    SaveOnboardingDraftStatus.Saved,
                                    cmd.Model.Id > 0 ? cmd.Model.Id : 1,
                                    "Draft saved successfully."));
                        sender.Setup(s => s.Send(It.IsAny<SaveOnboardingStep1Command>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((SaveOnboardingStep1Command cmd, System.Threading.CancellationToken _) =>
                            {
                                cmd.Session.FullName = cmd.Model.FullName;
                                cmd.Session.EmployeeID = cmd.Model.EmployeeID;
                                cmd.Session.Designation = cmd.Model.Designation;
                                cmd.Session.Department = cmd.Model.Department;
                                cmd.Session.Branch = cmd.Model.Branch;
                                cmd.Session.DeskNumber = cmd.Model.DeskNumber;
                                cmd.Session.JoinDate = cmd.Model.JoinDate;
                                cmd.Session.EmailAddress = cmd.Model.EmailAddress;
                                cmd.Session.ContactNumber = cmd.Model.ContactNumber;
                                cmd.Session.HODEmail = cmd.Model.HODEmail;
                                cmd.Session.Id = cmd.Session.Id > 0 ? cmd.Session.Id : 1;
                                cmd.Session.CurrentStep = 2;
                                return new SaveOnboardingStep1Result(
                                    SaveOnboardingStep1Status.ReadyForStep2,
                                    cmd.Session.Id,
                                    "Step 1 saved.");
                            });
                        sender.Setup(s => s.Send(It.IsAny<CompleteOnboardingHrStep2Command>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((CompleteOnboardingHrStep2Command cmd, System.Threading.CancellationToken _) =>
                            {
                                cmd.Session.Id = cmd.Session.Id > 0 ? cmd.Session.Id : 1;
                                return new CompleteOnboardingHrStep2Result(
                                    CompleteOnboardingHrStep2Status.Completed,
                                    cmd.Session.Id,
                                    "Onboarding form completed. Please sign below to submit to IT.");
                            });
                        sender.Setup(s => s.Send(It.IsAny<CompleteOnboardingItSetupCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((CompleteOnboardingItSetupCommand cmd, System.Threading.CancellationToken _) =>
                                new CompleteOnboardingItSetupResult(
                                    cmd.IsDraft ? CompleteOnboardingItSetupStatus.DraftSaved : CompleteOnboardingItSetupStatus.Completed,
                                    cmd.Session.Id > 0 ? cmd.Session.Id : 1,
                                    cmd.IsDraft ? "Draft saved successfully." : "IT setup complete."));
                        sender.Setup(s => s.Send(It.IsAny<UpdateOnboardingItStep3Command>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((UpdateOnboardingItStep3Command cmd, System.Threading.CancellationToken _) =>
                            {
                                cmd.Session.CurrentStep = 4;
                                return new UpdateOnboardingItStep3Result(
                                    cmd.IsDraft ? UpdateOnboardingItStep3Status.DraftSaved : UpdateOnboardingItStep3Status.ReadyForStep4,
                                    cmd.Session.Id > 0 ? cmd.Session.Id : 1,
                                    cmd.IsDraft ? "Draft saved successfully." : "Step 3 saved.");
                            });
                        sender.Setup(s => s.Send(It.IsAny<UpdateOnboardingItStep4Command>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((UpdateOnboardingItStep4Command cmd, System.Threading.CancellationToken _) =>
                            {
                                cmd.Session.CurrentStep = 5;
                                return new UpdateOnboardingItStep4Result(
                                    cmd.IsDraft ? UpdateOnboardingItStep4Status.DraftSaved : UpdateOnboardingItStep4Status.ReadyForStep5,
                                    cmd.Session.Id > 0 ? cmd.Session.Id : 1,
                                    cmd.IsDraft ? "Draft saved successfully." : "Step 4 saved.");
                            });
                        sender.Setup(s => s.Send(It.IsAny<EditOnboardingRequestCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((EditOnboardingRequestCommand cmd, System.Threading.CancellationToken _) =>
                                new EditOnboardingRequestResult(
                                    string.Equals(cmd.ActionType, "submit", StringComparison.OrdinalIgnoreCase)
                                        ? EditOnboardingRequestStatus.Submitted
                                        : EditOnboardingRequestStatus.Updated,
                                    cmd.Id,
                                    string.Equals(cmd.ActionType, "submit", StringComparison.OrdinalIgnoreCase)
                                        ? "Draft submitted."
                                        : "Request updated.",
                                    string.Equals(cmd.ActionType, "submit", StringComparison.OrdinalIgnoreCase)));
                        sender.Setup(s => s.Send(It.IsAny<GetEmailGroupIndexPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetEmailGroupIndexPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetEmailGroupIndexPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetEmailGroupCreatePageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetEmailGroupCreatePageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetEmailGroupCreatePageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetEmailGroupEditPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetEmailGroupEditPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetEmailGroupEditPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetEmailGroupMembersPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetEmailGroupMembersPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetEmailGroupMembersPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetSharedMailboxIndexPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetSharedMailboxIndexPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetSharedMailboxIndexPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetSharedMailboxCreatePageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetSharedMailboxCreatePageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetSharedMailboxCreatePageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetSharedMailboxEditPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetSharedMailboxEditPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetSharedMailboxEditPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetSharedMailboxMembersPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetSharedMailboxMembersPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetSharedMailboxMembersPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetServerFolderIndexPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetServerFolderIndexPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetServerFolderIndexPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetServerFolderCreatePageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetServerFolderCreatePageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetServerFolderCreatePageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetServerFolderEditPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetServerFolderEditPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetServerFolderEditPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetServerFolderMembersPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetServerFolderMembersPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetServerFolderMembersPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetDepartmentIndexPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetDepartmentIndexPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetDepartmentIndexPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetDepartmentCreatePageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetDepartmentCreatePageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetDepartmentCreatePageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetDepartmentEditPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetDepartmentEditPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetDepartmentEditPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetCompanyIndexPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetCompanyIndexPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetCompanyIndexPageQueryHandler(context).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetCompanyCreatePageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetCompanyCreatePageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetCompanyCreatePageQueryHandler().Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetCompanyEditPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetCompanyEditPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetCompanyEditPageQueryHandler(context).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetEmailContactIndexPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetEmailContactIndexPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetEmailContactIndexPageQueryHandler(context).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetEmailContactCreatePageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetEmailContactCreatePageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetEmailContactCreatePageQueryHandler().Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetEmailContactEditPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetEmailContactEditPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetEmailContactEditPageQueryHandler(context).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<SearchEmailContactsQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((SearchEmailContactsQuery cmd, System.Threading.CancellationToken ct) =>
                                new SearchEmailContactsQueryHandler(context).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetStandardSoftwareIndexPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetStandardSoftwareIndexPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetStandardSoftwareIndexPageQueryHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetStandardSoftwareCreatePageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetStandardSoftwareCreatePageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetStandardSoftwareCreatePageQueryHandler().Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetStandardSoftwareEditPageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetStandardSoftwareEditPageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetStandardSoftwareEditPageQueryHandler(context).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetStandardSoftwareKeysQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetStandardSoftwareKeysQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetStandardSoftwareKeysQueryHandler(context, MockEncryptionService.Object).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetStandardSoftwareEmployeesQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetStandardSoftwareEmployeesQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetStandardSoftwareEmployeesQueryHandler(context).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<CreateItAssetCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((CreateItAssetCommand cmd, System.Threading.CancellationToken ct) =>
                                new CreateItAssetCommandHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<EditItAssetCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((EditItAssetCommand cmd, System.Threading.CancellationToken ct) =>
                                new EditItAssetCommandHandler(context, scopedCurrentUser).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<AcknowledgeEmployeeSignatureCommand>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((AcknowledgeEmployeeSignatureCommand cmd, System.Threading.CancellationToken _) =>
                            {
                                var validator = new AcknowledgeEmployeeSignatureCommandValidator();
                                var validationResult = validator.Validate(cmd);
                                if (!validationResult.IsValid)
                                {
                                    throw new ValidationException(validationResult.Errors);
                                }

                                return Task.FromResult(ProcessResult<object>.Success(message: "Acknowledgement completed successfully."));
                            });
                        sender.Setup(s => s.Send(It.IsAny<GetSignatureAcknowledgePageQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetSignatureAcknowledgePageQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetSignatureAcknowledgePageQueryHandler(context, MockIntegrityService.Object).Handle(cmd, ct));
                        sender.Setup(s => s.Send(It.IsAny<GetSignaturePdfQuery>(), It.IsAny<System.Threading.CancellationToken>()))
                            .Returns((GetSignaturePdfQuery cmd, System.Threading.CancellationToken ct) =>
                                new GetSignaturePdfQueryHandler(context, MockPdfService.Object, MockIntegrityService.Object).Handle(cmd, ct));
                        args.Add(sender.Object);
                        continue;
                    }

                    // Generic mock for any other interface
                    var mockType = typeof(Mock<>).MakeGenericType(p.ParameterType);
                    args.Add(((Mock)Activator.CreateInstance(mockType)!).Object);
                }
                else args.Add(null!);
            }
            return args.ToArray();
        }

        /// <summary>
        /// Builds the value returned by the BuildRequestServices operation.
        /// </summary>
        private IServiceProvider BuildRequestServices()
        {
            var services = new ServiceCollection();
            var wizard = new Mock<IOnboardingWizardService>();
            wizard.Setup(w => w.GetFromSession(It.IsAny<ISession>()))
                .Returns((ISession s) =>
                {
                    var payload = s.GetString("OnboardingWizard");
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
            wizard.Setup(w => w.SaveToSession(It.IsAny<ISession>(), It.IsAny<OnboardingViewModel>()))
                .Callback((ISession s, OnboardingViewModel m) => s.SetString("OnboardingWizard", JsonConvert.SerializeObject(m)));
            wizard.Setup(w => w.MapStep1ToSession(It.IsAny<OnboardingViewModel>(), It.IsAny<OnboardingViewModel>()))
                .Callback((OnboardingViewModel source, OnboardingViewModel target) =>
                {
                    target.Id = source.Id;
                    target.FullName = source.FullName;
                    target.EmployeeID = source.EmployeeID;
                    target.Designation = source.Designation;
                    target.Department = source.Department;
                    target.Branch = source.Branch;
                    target.HODEmail = source.HODEmail;
                });
            wizard.Setup(w => w.MapRequestToSession(It.IsAny<OnboardingRequest>(), It.IsAny<OnboardingViewModel>()));
            wizard.Setup(w => w.MapSessionToRequest(It.IsAny<OnboardingViewModel>(), It.IsAny<OnboardingRequest>()));
            services.AddSingleton(wizard.Object);
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Builds the value returned by the BuildViewResult operation.
        /// </summary>
        private static ViewResult BuildViewResult(object? model)
        {
            return new ViewResult
            {
                ViewData = new ViewDataDictionary(
                    new EmptyModelMetadataProvider(),
                    new ModelStateDictionary())
                {
                    Model = model
                }
            };
        }

        /// <summary>
        /// Gets the data required by the GetMockUserManager operation.
        /// </summary>
        protected Mock<UserManager<ApplicationUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManager
                .Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((ClaimsPrincipal principal) =>
                {
                    var userName = principal.Identity?.Name ?? "testuser";
                    return new ApplicationUser
                    {
                        Id = userName,
                        UserName = userName,
                        Email = $"{userName}@example.com",
                        FullName = userName,
                        Signature = "test-signature",
                        IsActive = true
                    };
                });

            return userManager;
        }

        /// <summary>
        /// Runs the Dispose operation.
        /// </summary>
        public void Dispose()
        {
            // Cleanup database if needed
        }
    }

    /// <summary>
    /// Defines the MockSession class used by the IT Form application.
    /// </summary>
    public class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new();
        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _data.Keys;
        /// <summary>
        /// Runs the Clear operation.
        /// </summary>
        public void Clear() => _data.Clear();
        /// <summary>
        /// Runs the CommitAsync operation.
        /// </summary>
        public Task CommitAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        /// <summary>
        /// Runs the LoadAsync operation.
        /// </summary>
        public Task LoadAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        /// <summary>
        /// Runs the Remove operation.
        /// </summary>
        public void Remove(string key) => _data.Remove(key);
        /// <summary>
        /// Runs the Set operation.
        /// </summary>
        public void Set(string key, byte[] value) => _data[key] = value;
        /// <summary>
        /// Runs the TryGetValue operation.
        /// </summary>
        public bool TryGetValue(string key, out byte[] value) => _data.TryGetValue(key, out value!);
    }
}
