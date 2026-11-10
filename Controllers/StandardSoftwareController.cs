// Project comment: StandardSoftwareController.cs is part of the IT Form application source code.

using IT_Form.Application.Features.StandardSoftware;
using IT_Form.Application.Features.StandardSoftware.AddSoftwareKey;
using IT_Form.Application.Features.StandardSoftware.AddSoftwareLicense;
using IT_Form.Application.Features.StandardSoftware.AssignSoftwareKey;
using IT_Form.Application.Features.StandardSoftware.DeleteSoftwareKey;
using IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense;
using IT_Form.Application.Features.StandardSoftware.EditSoftwareLicense;
using IT_Form.Application.Features.StandardSoftware.ImportStandardSoftware;
using IT_Form.Application.Features.StandardSoftware.QuickAllocateSubscriptionSeat;
using IT_Form.Application.Features.StandardSoftware.UpdateSoftwareKey;
using CreateStandardSoftwareWriteCommand = IT_Form.Application.Features.StandardSoftware.Writes.CreateStandardSoftware.CreateStandardSoftwareCommand;
using DeleteStandardSoftwareWriteCommand = IT_Form.Application.Features.StandardSoftware.Writes.DeleteStandardSoftware.DeleteStandardSoftwareCommand;
using UpdateStandardSoftwareWriteCommand = IT_Form.Application.Features.StandardSoftware.Writes.UpdateStandardSoftware.UpdateStandardSoftwareCommand;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireIT")]
    /// <summary>
    /// Handles web requests for the StandardSoftware area.
    /// </summary>
    public class StandardSoftwareController : AppControllerBase
    {
        private readonly ISender _sender;

        public StandardSoftwareController(ISender sender) => _sender = sender;

        /// <summary>
        /// Runs the Index operation.
        /// </summary>
        public async Task<IActionResult> Index(string? searchString, bool? isMandatory, bool? isSubscription, int? companyId = null)
            => HandleProcessResult(await _sender.Send(new GetStandardSoftwareIndexPageQuery(searchString, isMandatory, isSubscription, companyId)));

        /// <summary>
        /// Creates data for the Create operation.
        /// </summary>
        public async Task<IActionResult> Create()
            => HandleProcessResult(await _sender.Send(new GetStandardSoftwareCreatePageQuery()));

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Creates data for the Create operation.
        /// </summary>
        public async Task<IActionResult> Create(StandardSoftwareEditorPageViewModel model)
        {
            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _sender.Send(new CreateStandardSoftwareWriteCommand(
                        model.SoftwareName,
                        model.IsMandatory,
                        model.IsFree,
                        model.IsActive,
                        model.HasMultipleVersions));

                    TempData["SuccessMessage"] = "Software created.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _sender.Send(new GetStandardSoftwareCreatePageQuery(model))));
        }

        /// <summary>
        /// Runs the Edit operation.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            return HandleProcessResult(await _sender.Send(new GetStandardSoftwareEditPageQuery(id.Value)));
        }

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the Edit operation.
        /// </summary>
        public async Task<IActionResult> Edit(int id, StandardSoftwareEditorPageViewModel model)
        {
            if (id != model.Id) return NotFound();
            return await ExecuteWithValidationResult(
                async () =>
                {
                    await _sender.Send(new UpdateStandardSoftwareWriteCommand(
                        id,
                        model.SoftwareName,
                        model.IsMandatory,
                        model.IsFree,
                        model.IsActive,
                        model.HasMultipleVersions));

                    TempData["SuccessMessage"] = "Software updated.";
                    return RedirectToAction(nameof(Index));
                },
                async () => HandleProcessResult(await _sender.Send(new GetStandardSoftwareEditPageQuery(id, model))));
        }

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Deletes data for the Delete operation.
        /// </summary>
        public async Task<IActionResult> Delete(int id)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    await _sender.Send(new DeleteStandardSoftwareWriteCommand(
                        id,
                        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));

                    TempData["SuccessMessage"] = "Software deleted.";
                    return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the AddLicense operation.
        /// </summary>
        public async Task<IActionResult> AddLicense(int standardSoftwareId, string softwareName,
            string? description, bool isSubscription, bool isFree, DateTime? expiryDate, int? contractLimit,
            string? vendorName, string? supportContact, int? selectedCompanyId)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                var result = await _sender.Send(new AddSoftwareLicenseCommand(
                    standardSoftwareId,
                    softwareName,
                    description,
                    isSubscription,
                    isFree,
                    expiryDate,
                    contractLimit,
                    vendorName,
                    supportContact,
                    selectedCompanyId.HasValue ? [selectedCompanyId.Value] : []));
                ArgumentNullException.ThrowIfNull(result);

                if (result.Status == AddSoftwareLicenseStatus.InvalidSubscriptionExpiry)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }
                if (result.Status == AddSoftwareLicenseStatus.NotFound) return NotFound();

                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the EditLicense operation.
        /// </summary>
        public async Task<IActionResult> EditLicense(int id, string softwareName,
            string? description, bool isSubscription, bool isFree, DateTime? expiryDate, int? contractLimit,
            string? vendorName, string? supportContact, int? selectedCompanyId)
            => await ExecuteWithValidationResult(
                async () =>
                {
                var result = await _sender.Send(new EditSoftwareLicenseCommand(
                    id,
                    softwareName,
                    description,
                    isSubscription,
                    isFree,
                    expiryDate,
                    contractLimit,
                    vendorName,
                    supportContact,
                    selectedCompanyId.HasValue ? [selectedCompanyId.Value] : []));
                ArgumentNullException.ThrowIfNull(result);
                if (result.Status == EditSoftwareLicenseStatus.NotFound) return Json(new { success = false, message = "Version not found." });
                if (result.Status == EditSoftwareLicenseStatus.InvalidSubscriptionExpiry) return Json(new { success = false, message = result.Message });
                if (result.Status == EditSoftwareLicenseStatus.InvalidSeatLimit) return Json(new { success = false, message = result.Message });
                return Json(new { success = true });
                },
                async () => Json(new { success = false, message = GetFirstModelStateErrorMessage() }));

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Deletes data for the DeleteLicense operation.
        /// </summary>
        public async Task<IActionResult> DeleteLicense(int id)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                var result = await _sender.Send(new DeleteSoftwareLicenseCommand(id));
                ArgumentNullException.ThrowIfNull(result);
                if (result.Status == DeleteSoftwareLicenseStatus.HasAssignedKeys)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                if (result.Status == DeleteSoftwareLicenseStatus.Deleted)
                {
                    TempData["SuccessMessage"] = result.Message;
                }
                return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        /// <summary>
        /// Runs the DownloadTemplate operation.
        /// </summary>
        public async Task<IActionResult> DownloadTemplate()
            => HandleProcessResult(await _sender.Send(new DownloadStandardSoftwareTemplateQuery()));

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the ImportExcel operation.
        /// </summary>
        public async Task<IActionResult> ImportExcel(IFormFile file)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                var result = await _sender.Send(new ImportStandardSoftwareCommand(file));
                ArgumentNullException.ThrowIfNull(result);

                if (!string.IsNullOrWhiteSpace(result.SuccessMessage))
                {
                    TempData["SuccessMessage"] = result.SuccessMessage;
                }
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }
                return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the AddKey operation.
        /// </summary>
        public async Task<IActionResult> AddKey(int softwareId, string keyValue)
            => await ExecuteWithValidationResult(
                async () =>
                {
                var result = await _sender.Send(new AddSoftwareKeyCommand(softwareId, keyValue));
                ArgumentNullException.ThrowIfNull(result);

                if (result.Status == AddSoftwareKeyStatus.Failed)
                    return BadRequest(result.Message);

                return Json(new { success = true });
                },
                async () => BadRequest("Key value is required."));

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Deletes data for the DeleteKey operation.
        /// </summary>
        public async Task<IActionResult> DeleteKey(int id)
            => await ExecuteWithValidationResult(
                async () =>
                {
                var result = await _sender.Send(new DeleteSoftwareKeyCommand(id));
                ArgumentNullException.ThrowIfNull(result);

                if (result.Status == DeleteSoftwareKeyStatus.NotFound) return NotFound();
                if (result.Status == DeleteSoftwareKeyStatus.AssignedBlocked) return BadRequest(result.Message);

                if (Request.Headers["Accept"].ToString().Contains("application/json"))
                    return Json(new { success = true });

                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
                },
                async () => BadRequest("Invalid key id."));

        [HttpGet]
        /// <summary>
        /// Gets the data required by the GetKeys operation.
        /// </summary>
        public async Task<IActionResult> GetKeys(int softwareId)
            => HandleJsonProcessResult(await _sender.Send(new GetStandardSoftwareKeysQuery(softwareId)));

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the QuickAllocateSeat operation.
        /// </summary>
        public async Task<IActionResult> QuickAllocateSeat(int softwareId)
            => await ExecuteWithValidationResult(
                async () =>
                {
                var result = await _sender.Send(new QuickAllocateSubscriptionSeatCommand(softwareId));
                ArgumentNullException.ThrowIfNull(result);

                if (result.Status == QuickAllocateSubscriptionSeatStatus.NotFound) return NotFound();
                if (result.Status == QuickAllocateSubscriptionSeatStatus.NotSubscription) return BadRequest(result.Message);

                return Json(new { success = true });
                },
                async () => BadRequest("Invalid software id."));

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Updates data for the UpdateKey operation.
        /// </summary>
        public async Task<IActionResult> UpdateKey(int id, string keyValue)
            => await ExecuteWithValidationResult(
                async () =>
                {
                var result = await _sender.Send(new UpdateSoftwareKeyCommand(id, keyValue));
                ArgumentNullException.ThrowIfNull(result);

                if (result.Status == UpdateSoftwareKeyStatus.NotFound) return NotFound();
                return Json(new { success = true });
                },
                async () => BadRequest("Key value is required."));

        [HttpPost, ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the AssignKey operation.
        /// </summary>
        public async Task<IActionResult> AssignKey(int id, int? onboardingRequestId)
            => await ExecuteWithValidationResult(
                async () =>
                {
                var result = await _sender.Send(new AssignSoftwareKeyCommand(id, onboardingRequestId));
                ArgumentNullException.ThrowIfNull(result);

                if (result.Status == AssignSoftwareKeyStatus.NotFound) return NotFound();
                if (result.Status == AssignSoftwareKeyStatus.EmployeeNotFound) return BadRequest(result.Message);
                return Json(new { success = true });
                },
                async () => BadRequest("Invalid key id."));

        [HttpGet]
        /// <summary>
        /// Gets the data required by the GetEmployees operation.
        /// </summary>
        public async Task<IActionResult> GetEmployees()
            => HandleJsonProcessResult(await _sender.Send(new GetStandardSoftwareEmployeesQuery()));
    }
}
