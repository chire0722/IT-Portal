// Project comment: ImportSharedMailboxesCommand.cs is part of the IT Form application source code.

using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.SharedMailbox.ImportSharedMailboxes
{
    /// <summary>
    /// Carries data needed to execute the ImportSharedMailboxes command.
    /// </summary>
    public sealed record ImportSharedMailboxesCommand(IFormFile File) : IRequest<ImportSharedMailboxesResult>;
}
