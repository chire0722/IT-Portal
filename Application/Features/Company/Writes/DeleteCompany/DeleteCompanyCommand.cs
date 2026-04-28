// Project comment: DeleteCompanyCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Company.Writes.DeleteCompany;

/// <summary>
/// Carries data needed to execute the DeleteCompany command.
/// </summary>
public sealed record DeleteCompanyCommand(int Id) : IRequest<Unit>;
