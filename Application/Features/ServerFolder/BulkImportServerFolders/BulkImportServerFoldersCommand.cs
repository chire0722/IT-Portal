using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.ServerFolder.BulkImportServerFolders
{
    public sealed record BulkImportServerFoldersCommand(IFormFile File) : IRequest<BulkImportServerFoldersResult>;
}
