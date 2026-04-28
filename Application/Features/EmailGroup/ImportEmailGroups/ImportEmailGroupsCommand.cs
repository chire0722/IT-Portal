// Project comment: ImportEmailGroupsCommand.cs is part of the IT Form application source code.

using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.EmailGroup.ImportEmailGroups
{
    /// <summary>
    /// Carries data needed to execute the ImportEmailGroups command.
    /// </summary>
    public sealed record ImportEmailGroupsCommand(IFormFile File) : IRequest<ImportEmailGroupsResult>;
}
