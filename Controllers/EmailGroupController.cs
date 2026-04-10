using ClosedXML.Excel;
using IT_Form.Application.Features.EmailGroup;
using IT_Form.Application.Features.EmailGroup.AddCurrentEmployeeToEmailGroup;
using IT_Form.Application.Features.EmailGroup.ImportEmailGroups;
using CreateEmailGroupWriteCommand = IT_Form.Application.Features.EmailGroups.Writes.CreateEmailGroup.CreateEmailGroupCommand;
using DeleteEmailGroupWriteCommand = IT_Form.Application.Features.EmailGroups.Writes.DeleteEmailGroup.DeleteEmailGroupCommand;
using UpdateEmailGroupWriteCommand = IT_Form.Application.Features.EmailGroups.Writes.UpdateEmailGroup.UpdateEmailGroupCommand;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireIT")]
    public class EmailGroupController : AppControllerBase
    {
        private readonly IMediator _mediator;

        public EmailGroupController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
            => HandleProcessResult(await _mediator.Send(new GetEmailGroupIndexPageQuery()));

        public async Task<IActionResult> Create()
            => HandleProcessResult(await _mediator.Send(new GetEmailGroupCreatePageQuery()));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmailGroupEditorPageViewModel model, int[] selectedCompanyIds)
        {
            if (model == null) return HandleProcessResult(await _mediator.Send(new GetEmailGroupCreatePageQuery()));

            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _mediator.Send(new CreateEmailGroupWriteCommand(
                        model.GroupName,
                        model.GroupEmail,
                        model.IsActive,
                        selectedCompanyIds ?? Array.Empty<int>()));

                    TempData["SuccessMessage"] = "Email Group created.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _mediator.Send(new GetEmailGroupCreatePageQuery(model, selectedCompanyIds))));
        }

        [NonAction]
        public Task<IActionResult> Create(EmailGroupEditorPageViewModel model)
            => Create(model, System.Array.Empty<int>());

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            return HandleProcessResult(await _mediator.Send(new GetEmailGroupEditPageQuery(id.Value)));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmailGroupEditorPageViewModel model, int[] selectedCompanyIds)
        {
            if (id != model.Id) return NotFound();

            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _mediator.Send(new UpdateEmailGroupWriteCommand(
                        id,
                        model.GroupName,
                        model.GroupEmail,
                        model.IsActive,
                        selectedCompanyIds ?? Array.Empty<int>()));

                    TempData["SuccessMessage"] = "Email Group updated.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _mediator.Send(new GetEmailGroupEditPageQuery(id, model, selectedCompanyIds))));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    await _mediator.Send(new DeleteEmailGroupWriteCommand(id));
                    TempData["SuccessMessage"] = "Email Group deleted.";
                    return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        public async Task<IActionResult> Members(int id, int? onboardingRequestId = null)
            => HandleProcessResult(await _mediator.Send(new GetEmailGroupMembersPageQuery(id, onboardingRequestId)));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCurrentEmployee(int id, int onboardingRequestId)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var result = await _mediator.Send(new AddCurrentEmployeeToEmailGroupCommand(id, onboardingRequestId));

                    switch (result.Status)
                    {
                        case AddCurrentEmployeeToEmailGroupStatus.GroupNotFound:
                            TempData["ErrorMessage"] = result.Message;
                            return RedirectToAction(nameof(Index));
                        case AddCurrentEmployeeToEmailGroupStatus.Forbidden:
                            return Forbid();
                        case AddCurrentEmployeeToEmailGroupStatus.InvalidEmployee:
                            TempData["ErrorMessage"] = result.Message;
                            return RedirectToAction(nameof(Members), new { id });
                        case AddCurrentEmployeeToEmailGroupStatus.AlreadyExists:
                            TempData["InfoMessage"] = result.Message;
                            return RedirectToAction(nameof(Members), new { id, onboardingRequestId });
                        default:
                            TempData["SuccessMessage"] = result.Message;
                            return RedirectToAction(nameof(Members), new { id, onboardingRequestId });
                    }
                },
                nameof(Members),
                new { id, onboardingRequestId });

        public async Task<IActionResult> Report(int? id, string? searchString, int page = 1)
            => HandleProcessResult(await _mediator.Send(new GetEmailGroupReportPageQuery(id, searchString, page)));

        public async Task<IActionResult> ExportCsv(int? id, string? searchString)
            => HandleProcessResult(await _mediator.Send(new ExportEmailGroupReportCsvQuery(id, searchString)));

        // GET: /EmailGroup/DownloadTemplate
        public async Task<IActionResult> DownloadTemplate()
            => HandleProcessResult(await _mediator.Send(new DownloadEmailGroupTemplateQuery()));


        // POST: /EmailGroup/ImportExcel
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile file)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var result = await _mediator.Send(new ImportEmailGroupsCommand(file));
                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = result.SuccessMessage;
                        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                        {
                            TempData["ErrorMessage"] = result.ErrorMessage;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }
                    return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });
    }
}
