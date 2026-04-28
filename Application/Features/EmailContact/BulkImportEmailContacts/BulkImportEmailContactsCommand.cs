// Project comment: BulkImportEmailContactsCommand.cs is part of the IT Form application source code.

using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.EmailContact.BulkImportEmailContacts
{
    /// <summary>
    /// Carries data needed to execute the BulkImportEmailContacts command.
    /// </summary>
    public sealed record BulkImportEmailContactsCommand(IFormFile File) : IRequest<BulkImportEmailContactsResult>;
}
