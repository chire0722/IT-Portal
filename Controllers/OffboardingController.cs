// Project comment: OffboardingController.cs is part of the IT Form application source code.

using System.Collections.Generic;
using System.Threading.Tasks;
using IT_Form.Application.Features.Offboarding.ActionWrites;
using IT_Form.Application.Features.Offboarding.Create;
using IT_Form.Application.Features.Offboarding.DeleteOffboardingRequest;
using IT_Form.Application.Features.Offboarding.DownloadOffboardingResignation;
using IT_Form.Application.Features.Offboarding.ExportBulkOffboardingPdf;
using IT_Form.Application.Features.Offboarding.ExportOffboardingCsv;
using IT_Form.Application.Features.Offboarding.GenerateOffboardingPdf;
using IT_Form.Application.Features.Offboarding.GetOffboardingDetails;
using IT_Form.Application.Features.Offboarding.GetOffboardingIndex;
using IT_Form.Application.Features.Offboarding.ItSetup;
using IT_Form.Application.Features.Offboarding.Step1;
using IT_Form.Application.Features.Offboarding.Step2;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IT_Form.Controllers;

[Authorize]
/// <summary>
/// Handles web requests for the Offboarding area.
/// </summary>
public class OffboardingController : AppControllerBase
{
    private readonly ISender _sender;

    public OffboardingController(ISender sender) => _sender = sender;

    /// <summary>
    /// Runs the Index operation.
    /// </summary>
    public async Task<IActionResult> Index(string searchString, RequestStatus? status, int page = 1)
        => HandleProcessResult(await _sender.Send(new GetOffboardingIndexQuery(searchString, status, page)));

    [Authorize(Policy = "RequireHR")]
    /// <summary>
    /// Runs the ExportCsv operation.
    /// </summary>
    public async Task<IActionResult> ExportCsv(string searchString, RequestStatus? status)
        => HandleProcessResult(await _sender.Send(new ExportOffboardingCsvQuery(searchString, status)));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireHR")]
    /// <summary>
    /// Runs the ExportBulkPdf operation.
    /// </summary>
    public async Task<IActionResult> ExportBulkPdf([FromBody] int[] ids)
        => HandleProcessResult(await _sender.Send(new ExportBulkOffboardingPdfCommand(ids)));

    /// <summary>
    /// Runs the Details operation.
    /// </summary>
    public async Task<IActionResult> Details(int? id)
        => id.HasValue ? HandleProcessResult(await _sender.Send(new GetOffboardingDetailsQuery(id.Value))) : NotFound();

    [Authorize(Policy = "RequireHR")]
    /// <summary>
    /// Creates data for the Create operation.
    /// </summary>
    public async Task<IActionResult> Create(int? onboardingRequestId = null)
        => HandleProcessResult(await _sender.Send(new GetOffboardingCreatePageQuery(onboardingRequestId)));

    [HttpGet]
    /// <summary>
    /// Gets the data required by the GetEmployeeDetails operation.
    /// </summary>
    public async Task<IActionResult> GetEmployeeDetails(int id)
    {
        var model = await _sender.Send(new GetOffboardingEmployeeDetailsQuery(id));
        return model != null ? Json(model) : NotFound();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireHR")]
    /// <summary>
    /// Creates data for the Create operation.
    /// </summary>
    public async Task<IActionResult> Create([Bind("OnboardingRequestId,FullName,EmployeeID,Department,Branch,LastDayOfWork,HODEmail,EmailAddress,OtherDepartmentDetails,OtherBranchDetails,AssignedHodId")] OffboardingRequest offboardingRequest, IFormFile? resignationLetter)
        => HandleProcessResult(await _sender.Send(new SubmitOffboardingCreateCommand(offboardingRequest, resignationLetter)));

    [HttpGet, Authorize(Policy = "RequireIT")]
    /// <summary>
    /// Runs the ITSetup operation.
    /// </summary>
    public async Task<IActionResult> ITSetup(int id)
        => HandleProcessResult(await _sender.Send(new StartOffboardingItSetupQuery(id)));

    /// <summary>
    /// Runs the Step1 operation.
    /// </summary>
    public async Task<IActionResult> Step1(int? id)
        => id.HasValue ? HandleProcessResult(await _sender.Send(new GetOffboardingStep1Query(id.Value))) : NotFound();

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireIT")]
    /// <summary>
    /// Runs the Step1 operation.
    /// </summary>
    public async Task<IActionResult> Step1(int id, string saveAction, OffboardingRequest request, IFormFile? hardwareDamageImage, IFormFile? hardwareReturnImage, List<int>? returnedAssetIds)
        => HandleProcessResult(await _sender.Send(new SubmitOffboardingStep1Command(id, saveAction, request, hardwareDamageImage, hardwareReturnImage, returnedAssetIds)));

    [HttpGet, Authorize(Policy = "RequireIT")]
    /// <summary>
    /// Runs the Step2 operation.
    /// </summary>
    public async Task<IActionResult> Step2(int? id)
        => id.HasValue ? HandleProcessResult(await _sender.Send(new GetOffboardingStep2Query(id.Value))) : NotFound();

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireIT")]
    /// <summary>
    /// Runs the Step2 operation.
    /// </summary>
    public async Task<IActionResult> Step2(int id, string saveAction, OffboardingRequest request)
        => HandleProcessResult(await _sender.Send(new SubmitOffboardingStep2Command(id, saveAction, request)));

    [HttpPost, ValidateAntiForgeryToken]
    /// <summary>
    /// Runs the SignOffHR operation.
    /// </summary>
    public async Task<IActionResult> SignOffHR(int id, string signatureBase64)
    {
        var result = await _sender.Send(new ProcessOffboardingHrSignatureCommand(id, signatureBase64));
        return ProcessResponse(result, "Details", id, autoOpenEmail: result.IsSuccess ? Url.Action("ComposeOffboardingEmail", "Notification", new { id = result.RequestId }) : null);
    }

    [HttpPost, ValidateAntiForgeryToken]
    /// <summary>
    /// Runs the ApproveChecker operation.
    /// </summary>
    public async Task<IActionResult> ApproveChecker(int id, string signatureBase64)
        => ProcessResponse(await _sender.Send(new ProcessOffboardingCheckerApprovalCommand(id, signatureBase64)), "Details", id,
            autoOpenEmail: Url.Action("ComposeOffboardingEmail", "Notification", new { id }));

    [HttpPost, ValidateAntiForgeryToken]
    /// <summary>
    /// Runs the SignOffManager operation.
    /// </summary>
    public async Task<IActionResult> SignOffManager(int id, string signatureBase64)
    {
        var result = await _sender.Send(new ProcessOffboardingManagerSignatureCommand(id, signatureBase64));
        return ProcessResponse(result, "Details", id, autoOpenEmail: result.IsSuccess ? Url.Action("ComposeOffboardingEmail", "Notification", new { id = result.RequestId }) : null);
    }

    [HttpPost, ValidateAntiForgeryToken]
    /// <summary>
    /// Runs the SignOffITAdmin operation.
    /// </summary>
    public async Task<IActionResult> SignOffITAdmin(int id, string signatureBase64)
        => ProcessResponse(await _sender.Send(new ProcessOffboardingItSignatureCommand(id, signatureBase64)), "Details", id);

    [HttpPost, ValidateAntiForgeryToken]
    /// <summary>
    /// Runs the SignOffEmployeeManual operation.
    /// </summary>
    public async Task<IActionResult> SignOffEmployeeManual(int id, string signatureBase64)
        => ProcessResponse(await _sender.Send(new ProcessOffboardingEmployeeManualSignatureCommand(id, signatureBase64)), "Details", id);

    [HttpPost, ValidateAntiForgeryToken]
    /// <summary>
    /// Runs the Reject operation.
    /// </summary>
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var result = await _sender.Send(new ProcessOffboardingRejectionCommand(id, reason));
        return ProcessResponse(result, "Details", id, autoOpenEmail: result.IsSuccess ? Url.Action("ComposeRejectionEmail", "Notification", new { id = result.RequestId, type = "Offboarding" }) : null);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireHR")]
    /// <summary>
    /// Deletes data for the Delete operation.
    /// </summary>
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _sender.Send(new DeleteOffboardingRequestCommand(id));
        if (result.IsNotFound) return NotFound();
        if (result.IsForbidden) return Forbid();
        return IsAjaxRequest()
            ? (result.IsSuccess ? Json(new { success = true, message = result.Message }) : Json(new { success = false, message = result.Message }))
            : (result.IsSuccess ? RedirectWithSuccess(result.Message, "Index", new { }) : RedirectWithError(result.Message, "Index", new { }));
    }

    /// <summary>
    /// Runs the GeneratePdf operation.
    /// </summary>
    public async Task<IActionResult> GeneratePdf(int id)
        => HandleProcessResult(await _sender.Send(new GenerateOffboardingPdfQuery(id)));

    [Authorize]
    /// <summary>
    /// Runs the DownloadResignation operation.
    /// </summary>
    public async Task<IActionResult> DownloadResignation(int id)
        => HandleProcessResult(await _sender.Send(new DownloadOffboardingResignationQuery(id)));

    [HttpPost, ValidateAntiForgeryToken]
    /// <summary>
    /// Runs the RegenerateToken operation.
    /// </summary>
    public async Task<IActionResult> RegenerateToken(int id)
        => ProcessResponse(await _sender.Send(new ProcessOffboardingTokenRegenerationCommand(id)), "Details", id);
}
