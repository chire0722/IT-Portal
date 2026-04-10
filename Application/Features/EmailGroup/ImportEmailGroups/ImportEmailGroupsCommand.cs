using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.EmailGroup.ImportEmailGroups
{
    public sealed record ImportEmailGroupsCommand(IFormFile File) : IRequest<ImportEmailGroupsResult>;
}
