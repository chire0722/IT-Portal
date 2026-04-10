using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.StandardSoftware.ImportStandardSoftware
{
    public sealed record ImportStandardSoftwareCommand(IFormFile File) : IRequest<ImportStandardSoftwareResult>;
}
