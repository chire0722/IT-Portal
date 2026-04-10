using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Company.Writes.CreateCompany;

public sealed record CreateCompanyCommand(
    string Name,
    string? Lot,
    bool IsLoginPageLogo,
    bool IsActive,
    IFormFile? Logo) : IRequest<int>;
