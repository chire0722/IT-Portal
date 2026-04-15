using ClosedXML.Excel;
using IT_Form.Application.Features.SharedMailbox;
using IT_Form.Application.Features.SharedMailbox.AddCurrentEmployeeToSharedMailbox;
using IT_Form.Application.Features.SharedMailbox.ImportSharedMailboxes;
using CreateSharedMailboxWriteCommand = IT_Form.Application.Features.SharedMailboxes.Writes.CreateSharedMailbox.CreateSharedMailboxCommand;
using DeleteSharedMailboxWriteCommand = IT_Form.Application.Features.SharedMailboxes.Writes.DeleteSharedMailbox.DeleteSharedMailboxCommand;
using UpdateSharedMailboxWriteCommand = IT_Form.Application.Features.SharedMailboxes.Writes.UpdateSharedMailbox.UpdateSharedMailboxCommand;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireIT")]
    public class SharedMailboxController : AppControllerBase
    {
        private readonly IMediator _mediator;

        public SharedMailboxController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index(int? companyId = null)
            => HandleProcessResult(await _mediator.Send(new GetSharedMailboxIndexPageQuery(companyId)));

        public async Task<IActionResult> Create()
            => HandleProcessResult(await _mediator.Send(new GetSharedMailboxCreatePageQuery()));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SharedMailboxEditorPageViewModel model, int[] selectedCompanyIds)
        {
            if (model == null) return HandleProcessResult(await _mediator.Send(new GetSharedMailboxCreatePageQuery()));
            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _mediator.Send(new CreateSharedMailboxWriteCommand(
                        model.MailboxName,
                        model.MailboxAddress,
                        model.IsActive,
                        selectedCompanyIds ?? Array.Empty<int>()));

                    TempData["SuccessMessage"] = "Shared Mailbox created.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _mediator.Send(new GetSharedMailboxCreatePageQuery(model, selectedCompanyIds))));
        }

        [NonAction]
        public Task<IActionResult> Create(SharedMailboxEditorPageViewModel model)
            => Create(model, System.Array.Empty<int>());

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            return HandleProcessResult(await _mediator.Send(new GetSharedMailboxEditPageQuery(id.Value)));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SharedMailboxEditorPageViewModel model, int[] selectedCompanyIds)
        {
            if (id != model.Id) return NotFound();
            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _mediator.Send(new UpdateSharedMailboxWriteCommand(
                        id,
                        model.MailboxName,
                        model.MailboxAddress,
                        model.IsActive,
                        selectedCompanyIds ?? Array.Empty<int>()));

                    TempData["SuccessMessage"] = "Shared Mailbox updated.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _mediator.Send(new GetSharedMailboxEditPageQuery(id, model, selectedCompanyIds))));
        }

        [NonAction]
        public Task<IActionResult> Edit(int id, SharedMailboxEditorPageViewModel model)
            => Edit(id, model, System.Array.Empty<int>());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    await _mediator.Send(new DeleteSharedMailboxWriteCommand(id));
                    TempData["SuccessMessage"] = "Shared Mailbox deleted.";
                    return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        public async Task<IActionResult> Members(int id, int? onboardingRequestId = null)
            => HandleProcessResult(await _mediator.Send(new GetSharedMailboxMembersPageQuery(id, onboardingRequestId)));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCurrentEmployee(int id, int onboardingRequestId)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var result = await _mediator.Send(new AddCurrentEmployeeToSharedMailboxCommand(id, onboardingRequestId));

                    switch (result.Status)
                    {
                        case AddCurrentEmployeeToSharedMailboxStatus.MailboxNotFound:
                            TempData["ErrorMessage"] = result.Message;
                            return RedirectToAction(nameof(Index));
                        case AddCurrentEmployeeToSharedMailboxStatus.Forbidden:
                            return Forbid();
                        case AddCurrentEmployeeToSharedMailboxStatus.InvalidEmployee:
                            TempData["ErrorMessage"] = result.Message;
                            return RedirectToAction(nameof(Members), new { id });
                        case AddCurrentEmployeeToSharedMailboxStatus.AlreadyExists:
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
            => HandleProcessResult(await _mediator.Send(new GetSharedMailboxReportPageQuery(id, searchString, page)));

        public async Task<IActionResult> ExportCsv(int? id, string? searchString)
            => HandleProcessResult(await _mediator.Send(new ExportSharedMailboxReportCsvQuery(id, searchString)));

        // GET: /SharedMailbox/DownloadTemplate
        public async Task<IActionResult> DownloadTemplate()
            => HandleProcessResult(await _mediator.Send(new DownloadSharedMailboxTemplateQuery()));


        // POST: /SharedMailbox/ImportExcel
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile file)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var result = await _mediator.Send(new ImportSharedMailboxesCommand(file));
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
