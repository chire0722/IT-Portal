using IT_Form.Application.Features.EmailContact.BulkImportEmailContacts;
using IT_Form.Application.Features.EmailContact;
using CreateEmailContactWriteCommand = IT_Form.Application.Features.EmailContact.Writes.CreateEmailContact.CreateEmailContactCommand;
using DeleteEmailContactWriteCommand = IT_Form.Application.Features.EmailContact.Writes.DeleteEmailContact.DeleteEmailContactCommand;
using UpdateEmailContactWriteCommand = IT_Form.Application.Features.EmailContact.Writes.UpdateEmailContact.UpdateEmailContactCommand;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System;

namespace IT_Form.Controllers
{
    [Authorize]
    public class EmailContactController : AppControllerBase
    {
        private readonly ISender _sender;

        public EmailContactController(ISender sender)
        {
            _sender = sender;
        }

        public async Task<IActionResult> Index()
            => HandleProcessResult(await _sender.Send(new GetEmailContactIndexPageQuery()));

        public async Task<IActionResult> Create()
            => HandleProcessResult(await _sender.Send(new GetEmailContactCreatePageQuery()));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmailContactEditorPageViewModel contact)
            => contact == null
                ? HandleProcessResult(await _sender.Send(new GetEmailContactCreatePageQuery()))
                : await ExecuteWithValidationResult(
                    async () =>
                    {
                        await _sender.Send(new CreateEmailContactWriteCommand(
                            contact.Name,
                            contact.EmailAddress,
                            contact.Department));

                        TempData["SuccessMessage"] = "Email contact created successfully.";
                        return RedirectToAction(nameof(Index));
                    },
                    async () => HandleProcessResult(await _sender.Send(new GetEmailContactCreatePageQuery(contact))));

        [Authorize(Policy = "RequireIT")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            return HandleProcessResult(await _sender.Send(new GetEmailContactEditPageQuery(id.Value)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireIT")]
        public async Task<IActionResult> Edit(int id, EmailContactEditorPageViewModel contact)
            => contact == null
                ? HandleProcessResult(await _sender.Send(new GetEmailContactEditPageQuery(id)))
                : id != contact.Id
                ? NotFound()
                : await ExecuteWithValidationResult(
                    async () =>
                    {
                        await _sender.Send(new UpdateEmailContactWriteCommand(
                            id,
                            contact.Name,
                            contact.EmailAddress,
                            contact.Department));

                        TempData["SuccessMessage"] = "Email contact updated successfully.";
                        return RedirectToAction(nameof(Index));
                    },
                    async () => HandleProcessResult(await _sender.Send(new GetEmailContactEditPageQuery(id, contact))));

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireIT")]
        public async Task<IActionResult> Delete(int id)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    await _sender.Send(new DeleteEmailContactWriteCommand(id));
                    TempData["SuccessMessage"] = "Email contact deleted successfully.";
                    return RedirectToAction(nameof(Index));
                },
                nameof(Index),
                new { });

        [HttpGet]
        public async Task<IActionResult> SearchContacts(string term)
            => HandleJsonProcessResult(await _sender.Send(new SearchEmailContactsQuery(term)));

        // GET: EmailContact/DownloadImportTemplate
        [Authorize(Policy = "RequireIT")]
        public async Task<IActionResult> DownloadImportTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("EmailContacts");
                
                worksheet.Cell(1, 1).Value = "Name*";
                worksheet.Cell(1, 2).Value = "Email Address*";
                worksheet.Cell(1, 3).Value = "Department";

                var headerRange = worksheet.Range(1, 1, 1, 3);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

                worksheet.Cell(2, 1).Value = "John Doe";
                worksheet.Cell(2, 2).Value = "john.doe@example.com";
                worksheet.Cell(2, 3).Value = "IT";

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Email_Contact_Import_Template.xlsx");
                }
            }
        }


        // POST: EmailContact/BulkImport
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireIT")]
        public async Task<IActionResult> BulkImport(IFormFile file)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                var result = await _sender.Send(new BulkImportEmailContactsCommand(file));

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
