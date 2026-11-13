// Project comment: OnboardingController.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup;
using IT_Form.Application.Features.Onboarding.Flows;
using IT_Form.Application.Features.Onboarding.GetOnboardingIndex;
using IT_Form.Application.Features.Onboarding.UpdateManageAccess;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IT_Form.Controllers
{
    [Authorize, EnableRateLimiting("fixed")]
    /// <summary>
    /// Handles web requests for the Onboarding area.
    /// </summary>
    public class OnboardingController : Controller
    {
        private readonly ISender _sender;
        public OnboardingController(ISender sender) => _sender = sender;

        /// <summary>
        /// Runs the Index operation.
        /// </summary>
        public async Task<IActionResult> Index(string searchString, RequestStatus? status, int page = 1) => HandleResult(await _sender.Send(new GetOnboardingIndexPageQuery(searchString, status, page)));
        [Authorize(Policy = "RequireHR")] public async Task<IActionResult> ExportCsv(string searchString, RequestStatus? status) => HandleResult(await _sender.Send(new ExportOnboardingCsvFileQuery(searchString, status)));
        [HttpPost, RequestSizeLimit(128 * 1024), ValidateAntiForgeryToken, Authorize(Policy = "RequireHR")] public async Task<IActionResult> ExportBulkPdf([FromBody] int[] ids) => HandleResult(await _sender.Send(new ExportBulkOnboardingPdfFileCommand(ids)));
        /// <summary>
        /// Runs the GeneratePdf operation.
        /// </summary>
        public async Task<IActionResult> GeneratePdf(int id) => HandleResult(await _sender.Send(new GenerateOnboardingPdfFileQuery(id)));
        /// <summary>
        /// Runs the Details operation.
        /// </summary>
        public async Task<IActionResult> Details(int id) => HandleResult(await _sender.Send(new GetOnboardingDetailsPageQuery(id)));
        [HttpGet, Authorize(Policy = "RequireIT")] public async Task<IActionResult> ManageAccess(int id) => HandleResult(await _sender.Send(new GetManageAccessPageQuery(id)));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireIT")] public async Task<IActionResult> ManageAccess(UpdateManageAccessCommand command) => HandleResult(await _sender.Send(new SubmitOnboardingManageAccessFlowCommand(command)));
        [Authorize(Policy = "RequireHROrIT")] public async Task<IActionResult> Edit(int id) => HandleResult(await _sender.Send(new GetOnboardingEditPageQuery(id)));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireHROrIT")] public async Task<IActionResult> Edit(int id, OnboardingRequest model, string actionType, List<int> SelectedStandardSoftwareIds, List<int> SelectedEmailGroupIds, List<int> SelectedSharedMailboxIds) => HandleResult(await _sender.Send(new SubmitEditOnboardingFlowCommand(id, model, actionType, SelectedStandardSoftwareIds ?? [], SelectedEmailGroupIds ?? [], SelectedSharedMailboxIds ?? [])));
        [Authorize(Policy = "RequireHROrIT")] public async Task<IActionResult> Delete(int id) => HandleResult(await _sender.Send(new GetOnboardingDeletePageQuery(id)));
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Policy = "RequireHROrIT")] public async Task<IActionResult> DeleteConfirmed(int id) => HandleResult(await _sender.Send(new SubmitDeleteOnboardingFlowCommand(id, IsAjaxRequest())));
        [Authorize(Policy = "RequireHR")] public async Task<IActionResult> Create() => HandleResult(await _sender.Send(new StartOnboardingCreateQuery()));
        [HttpGet, Authorize(Policy = "RequireHR")] public async Task<IActionResult> Step1() => HandleResult(await _sender.Send(new GetOnboardingStep1PageQuery()));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireHR")] public async Task<IActionResult> Step1(OnboardingStep1PageViewModel model) => HandleResult(await _sender.Send(new SubmitOnboardingStep1FlowCommand(model)));
        [HttpGet, Authorize(Policy = "RequireHR")] public async Task<IActionResult> Step2() => HandleResult(await _sender.Send(new GetOnboardingStep2PageQuery()));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireHR")] public async Task<IActionResult> Step2(OnboardingViewModel model) => HandleResult(await _sender.Send(new SubmitOnboardingStep2FlowCommand(model)));
        [HttpGet, Authorize(Policy = "RequireHR")] public async Task<IActionResult> SaveDraft() => HandleResult(await _sender.Send(new OpenOnboardingDraftEntryQuery()));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireHR")] public async Task<IActionResult> SaveDraft(OnboardingViewModel model) => HandleResult(await _sender.Send(new SubmitOnboardingDraftFlowCommand(model)));
        [HttpGet, Authorize(Policy = "RequireIT")] public async Task<IActionResult> ITSetup(int id) => HandleResult(await _sender.Send(new StartOnboardingItSetupQuery(id)));
        [HttpGet, Authorize(Policy = "RequireIT")] public async Task<IActionResult> Step3() => HandleResult(await _sender.Send(new GetOnboardingStep3PageQuery()));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireIT")] public async Task<IActionResult> Step3(OnboardingViewModel model, string saveAction) => HandleResult(await _sender.Send(new SubmitOnboardingStep3FlowCommand(model, saveAction)));
        [HttpGet, Authorize(Policy = "RequireIT")] public async Task<IActionResult> Step4() => HandleResult(await _sender.Send(new GetOnboardingStep4PageQuery()));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireIT")] public async Task<IActionResult> Step4(OnboardingViewModel model, string saveAction) => HandleResult(await _sender.Send(new SubmitOnboardingStep4FlowCommand(model, saveAction)));
        [HttpGet, Authorize(Policy = "RequireIT")] public async Task<IActionResult> Step5() => HandleResult(await _sender.Send(new GetOnboardingStep5PageQuery()));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireIT")] public async Task<IActionResult> Step5(CompleteOnboardingItSetupCommand command) => HandleResult(await _sender.Send(new SubmitOnboardingStep5FlowCommand(command)));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireIT")] public async Task<IActionResult> UpdateChecklist(int id, bool CheckPhysicalClean, bool CheckO365Setup, bool CheckWindowsLogin, bool CheckEmailSetup, bool CheckSpecificSoftware, bool CheckGeneralSoftware, bool CheckAbcGroup, bool CheckEmailMailGroup, bool CheckDomainJoin, bool CheckSharedDrive, bool CheckWiFiSetup, bool CheckPrinterSetup, bool CheckScanFolder, bool CheckVPNSetup, string? ChecklistRemarks) => HandleResult(await _sender.Send(new SubmitOnboardingChecklistFlowCommand(id, CheckPhysicalClean, CheckO365Setup, CheckWindowsLogin, CheckEmailSetup, CheckSpecificSoftware, CheckGeneralSoftware, CheckAbcGroup, CheckEmailMailGroup, CheckDomainJoin, CheckSharedDrive, CheckWiFiSetup, CheckPrinterSetup, CheckScanFolder, CheckVPNSetup, ChecklistRemarks)));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireHR")] public async Task<IActionResult> SignOffHR(int id, string signatureBase64) => HandleResult(await _sender.Send(new SubmitSignOffHrFlowCommand(id, signatureBase64)));
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Sign(int id, string signatureType, string signatureData) => HandleResult(await _sender.Send(new ProcessOnboardingSignatureFlowCommand(id, signatureType, signatureData)));
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Reject(int id, string reason) => HandleResult(await _sender.Send(new ProcessOnboardingRejectionFlowCommand(id, reason)));
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireHR")] public async Task<IActionResult> RegenerateToken(int id) => HandleResult(await _sender.Send(new ProcessOnboardingTokenRegenerationFlowCommand(id)));

        private IActionResult HandleResult<T>(ProcessResult<T> result)
        {
            if (result.IsNotFound) return NotFound();
            if (result.IsForbidden) return Forbid();
            if (!string.IsNullOrWhiteSpace(result.WarningMessage)) TempData["WarningMessage"] = result.WarningMessage;
            if (result.ShowItSummary) TempData["ShowITSummary"] = true;
            ApplyAutoOpenEmail(result.AutoOpenEmail);

            if (result.Data is IActionResult actionResult) return actionResult;
            if (result.Data is BinaryFilePayload filePayload) return File(filePayload.Content, filePayload.ContentType, filePayload.FileDownloadName);
            if (!string.IsNullOrWhiteSpace(result.RedirectAction))
            {
                if (result.AjaxAware && IsAjaxRequest()) return Json(new { success = result.IsSuccess, message = result.Message });
                if (!string.IsNullOrWhiteSpace(result.Message)) TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] = result.Message;
                return result.RequestId > 0 ? RedirectToAction(result.RedirectAction, new { id = result.RequestId }) : RedirectToAction(result.RedirectAction);
            }

            if (result.Data is GetOnboardingIndexResult index)
            {
                ViewData["CurrentFilter"] = index.CurrentFilter;
                ViewData["StatusFilter"] = index.StatusFilter;
                ViewData["SlaTarget"] = index.SlaTargetDays;
                return View(index.Requests);
            }

            return View(result.Data);
        }

        /// <summary>
        /// Runs the IsAjaxRequest operation.
        /// </summary>
        private bool IsAjaxRequest() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        /// <summary>
        /// Applies changes for the ApplyAutoOpenEmail operation.
        /// </summary>
        private void ApplyAutoOpenEmail(string? autoOpenEmail)
        {
            if (string.IsNullOrWhiteSpace(autoOpenEmail)) return;
            if (autoOpenEmail.StartsWith("ComposeEmail:", StringComparison.Ordinal))
                TempData["AutoOpenEmail"] = Url.Action("ComposeEmail", "Notification", new { id = int.Parse(autoOpenEmail["ComposeEmail:".Length..]) });
            else if (autoOpenEmail.StartsWith("ComposeRejectionEmail:", StringComparison.Ordinal))
            {
                var parts = autoOpenEmail.Split(':', StringSplitOptions.RemoveEmptyEntries);
                TempData["AutoOpenEmail"] = Url.Action("ComposeRejectionEmail", "Notification", new { id = int.Parse(parts[1]), type = parts[2] });
            }
            else TempData["AutoOpenEmail"] = autoOpenEmail;
        }
    }
}

