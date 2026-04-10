using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Offboarding.ActionWrites;
using IT_Form.Application.Features.Offboarding.Create;
using IT_Form.Application.Features.Offboarding.DeleteOffboardingRequest;
using IT_Form.Application.Features.Offboarding.DownloadOffboardingResignation;
using IT_Form.Application.Features.Offboarding.GenerateOffboardingPdf;
using IT_Form.Application.Features.Offboarding.GetOffboardingDetails;
using IT_Form.Application.Features.Offboarding.GetOffboardingIndex;
using IT_Form.Controllers;
using IT_Form.Models;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class OffboardingControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewModel_WhenQuerySucceeds()
    {
        var sender = new Mock<ISender>();
        var model = new OffboardingIndexPageViewModel();

        sender.Setup(x => x.Send(
                It.Is<GetOffboardingIndexQuery>(q => q.SearchString == "alice" && q.Page == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult<OffboardingIndexPageViewModel>.Success(model));

        var controller = CreateController(sender.Object);

        var result = await controller.Index("alice", null, 2);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
    }

    [Fact]
    public async Task Details_NullId_ReturnsNotFound()
    {
        var controller = CreateController(new Mock<ISender>().Object);

        var result = await controller.Details(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsView_WhenQuerySucceeds()
    {
        var sender = new Mock<ISender>();
        var model = new OffboardingDetailsPageViewModel { Id = 17, FullName = "Alicia Tan" };

        sender.Setup(x => x.Send(
                It.Is<GetOffboardingDetailsQuery>(q => q.Id == 17),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult<OffboardingDetailsPageViewModel>.Success(model));

        var controller = CreateController(sender.Object);

        var result = await controller.Details(17);

        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<OffboardingDetailsPageViewModel>(viewResult.Model);
        Assert.Equal(17, returnedModel.Id);
    }

    [Fact]
    public async Task CreateGet_ReturnsView_WhenQuerySucceeds()
    {
        var sender = new Mock<ISender>();
        var model = new OffboardingCreatePageViewModel { FullName = "Prefilled Employee" };

        sender.Setup(x => x.Send(
                It.Is<GetOffboardingCreatePageQuery>(q => q.OnboardingRequestId == 44),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult<OffboardingCreatePageViewModel>.Success(model));

        var controller = CreateController(sender.Object);

        var result = await controller.Create(44);

        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<OffboardingCreatePageViewModel>(viewResult.Model);
        Assert.Equal("Prefilled Employee", returnedModel.FullName);
    }

    [Fact]
    public async Task GetEmployeeDetails_ReturnsJson_WhenQueryReturnsModel()
    {
        var sender = new Mock<ISender>();
        var model = new OffboardingEmployeeDetailsViewModel { FullName = "Employee A" };

        sender.Setup(x => x.Send(
                It.Is<GetOffboardingEmployeeDetailsQuery>(q => q.Id == 9),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var controller = CreateController(sender.Object);

        var result = await controller.GetEmployeeDetails(9);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var returnedModel = Assert.IsType<OffboardingEmployeeDetailsViewModel>(jsonResult.Value);
        Assert.Equal("Employee A", returnedModel.FullName);
    }

    [Fact]
    public async Task GetEmployeeDetails_ReturnsNotFound_WhenQueryReturnsNull()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<GetOffboardingEmployeeDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OffboardingEmployeeDetailsViewModel?)null);

        var controller = CreateController(sender.Object);

        var result = await controller.GetEmployeeDetails(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreatePost_RedirectsToDetails_WhenCommandSucceeds()
    {
        var sender = new Mock<ISender>();
        var request = new OffboardingRequest { FullName = "New Employee" };
        var file = new Mock<IFormFile>().Object;

        sender.Setup(x => x.Send(
                It.Is<SubmitOffboardingCreateCommand>(c => ReferenceEquals(c.Request, request) && ReferenceEquals(c.ResignationLetter, file)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult<OffboardingCreatePageViewModel>.Success(message: "Created.", redirectAction: "Details", requestId: 51));

        var controller = CreateController(sender.Object, ajax: false);

        var result = await controller.Create(request, file);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(51, redirectResult.RouteValues!["id"]);
        Assert.Equal("Created.", controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Delete_Ajax_ReturnsJsonSuccess_WhenCommandSucceeds()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<DeleteOffboardingRequestCommand>(c => c.Id == 23),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResponse
            {
                IsSuccess = true,
                Message = "Deleted."
            });

        var controller = CreateController(sender.Object, ajax: true);

        var result = await controller.Delete(23);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.True(GetAnonymousValue<bool>(jsonResult.Value!, "success"));
        Assert.Equal("Deleted.", GetAnonymousValue<string>(jsonResult.Value!, "message"));
    }

        [Fact]
        public async Task Delete_ReturnsForbid_WhenCommandIsForbidden()
        {
            var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<DeleteOffboardingRequestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResponse
            {
                IsForbidden = true
            });

        var controller = CreateController(sender.Object);

        var result = await controller.Delete(23);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task SignOffHr_Ajax_ReturnsJsonAndSetsAutoOpenEmail_WhenCommandSucceeds()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ProcessOffboardingHrSignatureCommand>(c => c.Id == 77 && c.SignatureBase64 == "sig"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResponse
            {
                IsSuccess = true,
                Message = "Signed.",
                RequestId = 77
            });

        var controller = CreateController(sender.Object, ajax: true, composeUrl: "http://test/offboarding-email");

        var result = await controller.SignOffHR(77, "sig");

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.True(GetAnonymousValue<bool>(jsonResult.Value!, "success"));
        Assert.Equal("Signed.", GetAnonymousValue<string>(jsonResult.Value!, "message"));
        Assert.Equal("http://test/offboarding-email", controller.TempData["AutoOpenEmail"]);
    }

    [Fact]
    public async Task Reject_NonAjax_RedirectsWithError_WhenCommandFails()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ProcessOffboardingRejectionCommand>(c => c.Id == 88 && c.Reason == "missing"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResponse
            {
                Message = "Cannot reject.",
                RequestId = 88
            });

        var controller = CreateController(sender.Object, ajax: false, composeUrl: "http://test/rejection-email");

        var result = await controller.Reject(88, "missing");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(88, redirectResult.RouteValues!["id"]);
        Assert.Equal("Cannot reject.", controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task RegenerateToken_Ajax_ReturnsJsonFailure_WhenCommandFails()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ProcessOffboardingTokenRegenerationCommand>(c => c.Id == 15),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResponse
            {
                Message = "Token can only be regenerated for requests awaiting employee signature.",
                RequestId = 15
            });

        var controller = CreateController(sender.Object, ajax: true);

        var result = await controller.RegenerateToken(15);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.False(GetAnonymousValue<bool>(jsonResult.Value!, "success"));
        Assert.Equal(
            "Token can only be regenerated for requests awaiting employee signature.",
            GetAnonymousValue<string>(jsonResult.Value!, "message"));
    }

    [Fact]
    public async Task GeneratePdf_ReturnsFileResult_WhenQuerySucceeds()
    {
        var sender = new Mock<ISender>();
        var filePayload = new BinaryFilePayload([0x25, 0x50, 0x44, 0x46], "application/pdf", "offboarding.pdf");

        sender.Setup(x => x.Send(
                It.Is<GenerateOffboardingPdfQuery>(q => q.Id == 61),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult<BinaryFilePayload>.Success(filePayload));

        var controller = CreateController(sender.Object);

        var result = await controller.GeneratePdf(61);

        var returnedFile = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", returnedFile.ContentType);
    }

    [Fact]
    public async Task DownloadResignation_ReturnsNotFound_WhenQueryDoesNotFindFile()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<DownloadOffboardingResignationQuery>(q => q.Id == 404),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult<BinaryFilePayload>.NotFound());

        var controller = CreateController(sender.Object);

        var result = await controller.DownloadResignation(404);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ApproveChecker_SetsAutoOpenEmail_WhenCommandSucceeds()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ProcessOffboardingCheckerApprovalCommand>(c => c.Id == 12 && c.SignatureBase64 == "sig"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResponse
            {
                IsSuccess = true,
                Message = "Approved.",
                RequestId = 12
            });

        var controller = CreateController(sender.Object, ajax: false, composeUrl: "http://test/checker-email");

        var result = await controller.ApproveChecker(12, "sig");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal("http://test/checker-email", controller.TempData["AutoOpenEmail"]);
    }

    private static OffboardingController CreateController(ISender sender, bool ajax = false, string composeUrl = "http://test/email")
    {
        var controller = new OffboardingController(sender);
        var httpContext = new DefaultHttpContext();

        if (ajax)
        {
            httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns(composeUrl);
        controller.Url = url.Object;

        return controller;
    }

    private static T GetAnonymousValue<T>(object source, string propertyName)
        => (T)source.GetType().GetProperty(propertyName)!.GetValue(source)!;
}
