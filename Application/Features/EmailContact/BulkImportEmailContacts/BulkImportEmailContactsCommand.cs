using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.EmailContact.BulkImportEmailContacts
{
    public sealed record BulkImportEmailContactsCommand(IFormFile File) : IRequest<BulkImportEmailContactsResult>;
}
