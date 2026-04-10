using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.SharedMailbox.ImportSharedMailboxes
{
    public sealed record ImportSharedMailboxesCommand(IFormFile File) : IRequest<ImportSharedMailboxesResult>;
}
