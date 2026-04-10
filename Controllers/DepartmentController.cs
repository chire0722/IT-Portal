using IT_Form.Application.Features.Department.ImportDepartments;
using IT_Form.Application.Features.Department;
using CreateDepartmentWriteCommand = IT_Form.Application.Features.Departments.Writes.CreateDepartment.CreateDepartmentCommand;
using DeleteDepartmentWriteCommand = IT_Form.Application.Features.Departments.Writes.DeleteDepartment.DeleteDepartmentCommand;
using UpdateDepartmentWriteCommand = IT_Form.Application.Features.Departments.Writes.UpdateDepartment.UpdateDepartmentCommand;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireIT")]
    public class DepartmentController : AppControllerBase
    {
        private readonly IMediator _mediator;

        public DepartmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
            => HandleProcessResult(await _mediator.Send(new GetDepartmentIndexPageQuery()));

        public async Task<IActionResult> Create()
            => HandleProcessResult(await _mediator.Send(new GetDepartmentCreatePageQuery()));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentEditorPageViewModel model, int[] selectedCompanyIds)
        {
            if (model == null) return HandleProcessResult(await _mediator.Send(new GetDepartmentCreatePageQuery()));

            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _mediator.Send(new CreateDepartmentWriteCommand(
                        model.Name,
                        model.Description,
                        model.IsActive,
                        selectedCompanyIds ?? Array.Empty<int>()));

                    TempData["SuccessMessage"] = "Department created.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _mediator.Send(new GetDepartmentCreatePageQuery(model, selectedCompanyIds))));
        }

        [NonAction]
        public Task<IActionResult> Create(DepartmentEditorPageViewModel model)
            => Create(model, System.Array.Empty<int>());

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            return HandleProcessResult(await _mediator.Send(new GetDepartmentEditPageQuery(id.Value)));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DepartmentEditorPageViewModel model, int[] selectedCompanyIds)
        {
            if (id != model.Id) return NotFound();

            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _mediator.Send(new UpdateDepartmentWriteCommand(
                        id,
                        model.Name,
                        model.Description,
                        model.IsActive,
                        selectedCompanyIds ?? Array.Empty<int>()));

                    TempData["SuccessMessage"] = "Department updated.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _mediator.Send(new GetDepartmentEditPageQuery(id, model, selectedCompanyIds))));
        }

        [NonAction]
        public Task<IActionResult> Edit(int id, DepartmentEditorPageViewModel model)
            => Edit(id, model, System.Array.Empty<int>());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    await _mediator.Send(new DeleteDepartmentWriteCommand(id));
                    TempData["SuccessMessage"] = "Department deleted.";
                    return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        // GET: /Department/DownloadTemplate
        public async Task<IActionResult> DownloadTemplate()
            => HandleProcessResult(await _mediator.Send(new DownloadDepartmentTemplateQuery()));


        // POST: /Department/ImportExcel
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile file)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                var result = await _mediator.Send(new ImportDepartmentsCommand(file));
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
