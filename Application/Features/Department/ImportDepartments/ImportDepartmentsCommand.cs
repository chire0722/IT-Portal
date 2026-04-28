// Project comment: ImportDepartmentsCommand.cs is part of the IT Form application source code.

using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Department.ImportDepartments
{
    /// <summary>
    /// Carries data needed to execute the ImportDepartments command.
    /// </summary>
    public sealed record ImportDepartmentsCommand(IFormFile File) : IRequest<ImportDepartmentsResult>;
}
