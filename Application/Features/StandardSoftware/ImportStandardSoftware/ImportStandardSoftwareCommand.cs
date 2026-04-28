// Project comment: ImportStandardSoftwareCommand.cs is part of the IT Form application source code.

using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.StandardSoftware.ImportStandardSoftware
{
    /// <summary>
    /// Carries data needed to execute the ImportStandardSoftware command.
    /// </summary>
    public sealed record ImportStandardSoftwareCommand(IFormFile File) : IRequest<ImportStandardSoftwareResult>;
}
