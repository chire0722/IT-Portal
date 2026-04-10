using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Department.ImportDepartments
{
    public sealed record ImportDepartmentsCommand(IFormFile File) : IRequest<ImportDepartmentsResult>;
}
