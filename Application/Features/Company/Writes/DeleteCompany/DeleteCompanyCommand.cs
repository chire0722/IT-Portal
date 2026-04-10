using MediatR;

namespace IT_Form.Application.Features.Company.Writes.DeleteCompany;

public sealed record DeleteCompanyCommand(int Id) : IRequest<Unit>;
