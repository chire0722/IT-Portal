// Project comment: BulkImportEmailContactsCommandHandler.cs is part of the IT Form application source code.

using ClosedXML.Excel;
using IT_Form.Data;
using IT_Form.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailContact.BulkImportEmailContacts
{
    /// <summary>
    /// Handles the BulkImportEmailContactsCommand command workflow.
    /// </summary>
    public sealed class BulkImportEmailContactsCommandHandler : IRequestHandler<BulkImportEmailContactsCommand, BulkImportEmailContactsResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BulkImportEmailContactsCommandHandler>? _logger;

        public BulkImportEmailContactsCommandHandler(
            ApplicationDbContext context,
            ILogger<BulkImportEmailContactsCommandHandler>? logger = null)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<BulkImportEmailContactsResult> Handle(BulkImportEmailContactsCommand request, CancellationToken cancellationToken)
        {
            if (!FileUploadHelper.IsValidExcelFile(request.File, out var validationError))
            {
                return new BulkImportEmailContactsResult(false, null, null, validationError);
            }

            try
            {
                using var stream = new MemoryStream();
                await request.File.CopyToAsync(stream, cancellationToken);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rangeUsed = worksheet.RangeUsed();
                if (rangeUsed == null)
                {
                    return new BulkImportEmailContactsResult(false, null, null, "The uploaded workbook contains no data.");
                }

                if (rangeUsed.RowCount() > 5001 || rangeUsed.ColumnCount() > 20)
                {
                    return new BulkImportEmailContactsResult(false, null, null, "Email contact import is limited to 5,000 data rows and 20 columns.");
                }

                var rows = rangeUsed.RowsUsed().Skip(1);

                int successCount = 0;
                int skipCount = 0;
                int errorCount = 0;

                foreach (var row in rows)
                {
                    var name = row.Cell(1).GetValue<string>()?.Trim();
                    var email = row.Cell(2).GetValue<string>()?.Trim();
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
                    {
                        continue;
                    }

                    if (await _context.EmailContacts.AnyAsync(c => c.EmailAddress == email, cancellationToken))
                    {
                        skipCount++;
                        continue;
                    }

                    try
                    {
                        var contact = new IT_Form.Models.EmailContact
                        {
                            Name = name,
                            EmailAddress = email,
                            Department = row.Cell(3).GetValue<string>()?.Trim(),
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.EmailContacts.Add(contact);
                        successCount++;
                    }
                    catch
                    {
                        errorCount++;
                    }
                }

                if (successCount > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                var successMessage = successCount > 0 ? $"Successfully imported {successCount} contacts." : null;
                var infoMessage = skipCount > 0 ? $"{skipCount} contacts were skipped as they already exist." : null;
                var errorMessage = errorCount > 0 ? $"Failed to import {errorCount} rows due to errors." : null;

                return new BulkImportEmailContactsResult(true, successMessage, infoMessage, errorMessage);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to import email contacts from uploaded workbook.");
                return new BulkImportEmailContactsResult(false, null, null, "The uploaded workbook could not be processed.");
            }
        }
    }
}
