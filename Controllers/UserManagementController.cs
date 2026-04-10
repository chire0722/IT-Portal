using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.UserManagement;
using CreateUserManagementWriteCommand = IT_Form.Application.Features.UserManagement.Writes.CreateUserManagement.CreateUserManagementCommand;
using DeleteUserManagementWriteCommand = IT_Form.Application.Features.UserManagement.Writes.DeleteUserManagement.DeleteUserManagementCommand;
using ToggleUserManagementActiveWriteCommand = IT_Form.Application.Features.UserManagement.Writes.ToggleUserManagementActive.ToggleUserManagementActiveCommand;
using UpdateUserManagementWriteCommand = IT_Form.Application.Features.UserManagement.Writes.UpdateUserManagement.UpdateUserManagementCommand;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mapster;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireIT")]
    public class UserManagementController : AppControllerBase
    {
        private readonly ISender _sender;

        public UserManagementController(ISender sender) => _sender = sender;

        public async Task<IActionResult> Index()
            => HandleUserManagementIndexPage(await _sender.Send(new GetUserManagementIndexPageQuery()));

        [HttpGet]
        public async Task<IActionResult> Create()
            => HandleUserManagementEditorPage(await _sender.Send(new GetUserManagementCreatePageQuery()), "Create");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserManagementEditorPageViewModel model)
        {
            if (model == null)
                return HandleUserManagementEditorPage(await _sender.Send(new GetUserManagementCreatePageQuery()), "Create");

            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _sender.Send(model.Adapt<CreateUserManagementWriteCommand>());
                    TempData["SuccessMessage"] = $"User {model.FullName} created successfully!";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleUserManagementEditorPage(await _sender.Send(new GetUserManagementCreatePageQuery(model)), "Create"));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
            => HandleUserManagementEditorPage(await _sender.Send(new GetUserManagementEditPageQuery(id)), "Edit");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserManagementEditorPageViewModel model)
        {
            if (model == null)
                return HandleUserManagementEditorPage(await _sender.Send(new GetUserManagementEditPageQuery(id)), "Edit");

            return await ExecuteWithValidationResult(
                async () =>
                {
                    var command = model.Adapt<UpdateUserManagementWriteCommand>() with { Id = id };
                    await _sender.Send(command);
                    TempData["SuccessMessage"] = $"User {model.FullName} updated successfully!";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleUserManagementEditorPage(await _sender.Send(new GetUserManagementEditPageQuery(id, model)), "Edit"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var result = await _sender.Send(new ToggleUserManagementActiveWriteCommand(id));
                    TempData["SuccessMessage"] = result.IsActive
                        ? $"User {result.FullName} has been activated."
                        : $"User {result.FullName} has been deactivated.";
                    return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var fullName = await _sender.Send(new DeleteUserManagementWriteCommand(id));
                    TempData["SuccessMessage"] = $"User {fullName} has been permanently deleted.";
                    return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        private IActionResult HandleUserManagementIndexPage(ProcessResult<List<UserManagementIndexListItem>> result)
        {
            if (result.IsNotFound)
                return NotFound();

            if (result.IsForbidden)
                return Forbid();

            return View(result.Data ?? []);
        }

        private IActionResult HandleUserManagementEditorPage(
            ProcessResult<UserManagementEditorPageViewModel> result,
            string viewName)
        {
            if (result.IsNotFound)
                return NotFound();

            if (result.IsForbidden)
                return Forbid();

            return View(viewName, result.Data);
        }
    }
}
