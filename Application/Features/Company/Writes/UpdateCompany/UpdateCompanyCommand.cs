using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Company.Writes.UpdateCompany;

public sealed record UpdateCompanyCommand(
    int Id,
    string Name,
    string? Lot,
    bool IsLoginPageLogo,
    bool IsActive,
    IFormFile? Logo) : IRequest<int>;
