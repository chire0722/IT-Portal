using IT_Form.Application.Features.ServerFolder;
using IT_Form.Application.Features.ServerFolder.AddCurrentEmployeeToServerFolder;
using IT_Form.Application.Features.ServerFolder.BulkImportServerFolders;
using CreateServerFolderWriteCommand = IT_Form.Application.Features.ServerFolders.Writes.CreateServerFolder.CreateServerFolderCommand;
using UpdateServerFolderWriteCommand = IT_Form.Application.Features.ServerFolders.Writes.UpdateServerFolder.UpdateServerFolderCommand;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireIT")]
    public class ServerFolderController : AppControllerBase
    {
        private readonly IMediator _mediator;

        public ServerFolderController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index(int? companyId = null)
            => HandleProcessResult(await _mediator.Send(new GetServerFolderIndexPageQuery(companyId)));

        public async Task<IActionResult> Create()
            => HandleProcessResult(await _mediator.Send(new GetServerFolderCreatePageQuery()));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServerFolderEditorPageViewModel model, int[] selectedCompanyIds)
        {
            if (model == null) return HandleProcessResult(await _mediator.Send(new GetServerFolderCreatePageQuery()));
            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _mediator.Send(new CreateServerFolderWriteCommand(
                        model.FolderName,
                        model.IsActive,
                        selectedCompanyIds ?? Array.Empty<int>()));

                    TempData["SuccessMessage"] = "ServerFolder created successfully.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _mediator.Send(new GetServerFolderCreatePageQuery(model, selectedCompanyIds))));
        }

        [NonAction]
        public Task<IActionResult> Create(ServerFolderEditorPageViewModel model)
            => Create(model, System.Array.Empty<int>());

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            return HandleProcessResult(await _mediator.Send(new GetServerFolderEditPageQuery(id.Value)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServerFolderEditorPageViewModel model, int[] selectedCompanyIds)
        {
            if (id != model.Id) return NotFound();
            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _mediator.Send(new UpdateServerFolderWriteCommand(
                        id,
                        model.FolderName,
                        model.IsActive,
                        selectedCompanyIds ?? Array.Empty<int>()));

                    TempData["SuccessMessage"] = "ServerFolder updated successfully.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _mediator.Send(new GetServerFolderEditPageQuery(id, model, selectedCompanyIds))));
        }

        public async Task<IActionResult> Report(int? id, string? searchString, int page = 1)
            => HandleProcessResult(await _mediator.Send(new GetServerFolderReportPageQuery(id, searchString, page)));

        public async Task<IActionResult> Members(int id, int? onboardingRequestId = null)
            => HandleProcessResult(await _mediator.Send(new GetServerFolderMembersPageQuery(id, onboardingRequestId)));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCurrentEmployee(int id, int onboardingRequestId, bool canRead = true, bool canModify = false)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var result = await _mediator.Send(new AddCurrentEmployeeToServerFolderCommand(id, onboardingRequestId, canRead, canModify));

                    switch (result.Status)
                    {
                        case AddCurrentEmployeeToServerFolderStatus.FolderNotFound:
                            TempData["ErrorMessage"] = result.Message;
                            return RedirectToAction(nameof(Index));
                        case AddCurrentEmployeeToServerFolderStatus.Forbidden:
                            return Forbid();
                        case AddCurrentEmployeeToServerFolderStatus.InvalidEmployee:
                            TempData["ErrorMessage"] = result.Message;
                            return RedirectToAction(nameof(Members), new { id });
                        case AddCurrentEmployeeToServerFolderStatus.Updated:
                            TempData["InfoMessage"] = result.Message;
                            return RedirectToAction(nameof(Members), new { id, onboardingRequestId });
                        default:
                            TempData["SuccessMessage"] = result.Message;
                            return RedirectToAction(nameof(Members), new { id, onboardingRequestId });
                    }
                },
                nameof(Members),
                new { id, onboardingRequestId });

        public async Task<IActionResult> ExportCsv(int? id, string? searchString)
            => HandleProcessResult(await _mediator.Send(new ExportServerFolderReportCsvQuery(id, searchString)));

        // GET: /ServerFolder/DownloadTemplate
        public async Task<IActionResult> DownloadTemplate()
            => HandleProcessResult(await _mediator.Send(new DownloadServerFolderTemplateQuery()));

        // POST: ServerFolder/BulkImport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkImport(IFormFile file)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var result = await _mediator.Send(new BulkImportServerFoldersCommand(file));

                    if (!string.IsNullOrWhiteSpace(result.SuccessMessage))
                    {
                        TempData["SuccessMessage"] = result.SuccessMessage;
                    }
                    if (!string.IsNullOrWhiteSpace(result.InfoMessage))
                    {
                        TempData["InfoMessage"] = result.InfoMessage;
                    }
                    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }

                    return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });
    }
}
