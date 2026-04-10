using ClosedXML.Excel;
using IT_Form.Application.Features.Company.SyncCompanyNames;
using IT_Form.Application.Features.Company;
using CreateCompanyWriteCommand = IT_Form.Application.Features.Company.Writes.CreateCompany.CreateCompanyCommand;
using DeleteCompanyWriteCommand = IT_Form.Application.Features.Company.Writes.DeleteCompany.DeleteCompanyCommand;
using UpdateCompanyWriteCommand = IT_Form.Application.Features.Company.Writes.UpdateCompany.UpdateCompanyCommand;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MediatR;


namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireIT")]
    [Authorize(Policy = "RequireHROrIT")]
    public class CompanyController : AppControllerBase
    {
        private readonly ISender _sender;

        public CompanyController(ISender sender)
        {
            _sender = sender;
        }

        public async Task<IActionResult> Index()
            => HandleProcessResult(await _sender.Send(new GetCompanyIndexPageQuery()));

        public async Task<IActionResult> Create()
            => HandleProcessResult(await _sender.Send(new GetCompanyCreatePageQuery()));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyEditorPageViewModel model, IFormFile? logo = null)
        {
            if (model == null) return HandleProcessResult(await _sender.Send(new GetCompanyCreatePageQuery()));

            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _sender.Send(new CreateCompanyWriteCommand(
                        model.Name,
                        model.Lot,
                        model.IsLoginPageLogo,
                        model.IsActive,
                        logo));

                    TempData["SuccessMessage"] = "Company created.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _sender.Send(new GetCompanyCreatePageQuery(model))));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            return HandleProcessResult(await _sender.Send(new GetCompanyEditPageQuery(id.Value)));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CompanyEditorPageViewModel model, IFormFile? logo = null)
        {
            if (id != model.Id) return NotFound();

            return await ExecuteWithValidationResult(
                async () =>
                {
                    var syncedCount = await _sender.Send(new UpdateCompanyWriteCommand(
                        id,
                        model.Name,
                        model.Lot,
                        model.IsLoginPageLogo,
                        model.IsActive,
                        logo));

                    TempData["SuccessMessage"] = syncedCount > 0
                        ? $"Company updated and synchronized {syncedCount} related record(s)."
                        : "Company updated.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _sender.Send(new GetCompanyEditPageQuery(id, model))));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    await _sender.Send(new DeleteCompanyWriteCommand(id));
                    TempData["SuccessMessage"] = "Company deleted.";
                    return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireIT")]
        public async Task<IActionResult> SyncCompanyNames()
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                var result = await _sender.Send(new SyncCompanyNamesCommand());
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });
    }
}
