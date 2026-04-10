using ClosedXML.Excel;
using IT_Form.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailContact.BulkImportEmailContacts
{
    public sealed class BulkImportEmailContactsCommandHandler : IRequestHandler<BulkImportEmailContactsCommand, BulkImportEmailContactsResult>
    {
        private readonly ApplicationDbContext _context;

        public BulkImportEmailContactsCommandHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BulkImportEmailContactsResult> Handle(BulkImportEmailContactsCommand request, CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return new BulkImportEmailContactsResult(false, null, null, "Please select a valid Excel file.");
            }

            try
            {
                using var stream = new MemoryStream();
                await request.File.CopyToAsync(stream, cancellationToken);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rangeUsed = worksheet.RangeUsed();
                var rows = rangeUsed != null ? rangeUsed.RowsUsed().Skip(1) : Enumerable.Empty<IXLRangeRow>();

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
                return new BulkImportEmailContactsResult(false, null, null, "Error processing file: " + ex.Message);
            }
        }
    }
}
