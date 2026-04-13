using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Company.Writes.CreateCompany;

public sealed record CreateCompanyCommand(
    string Name,
    string? Lot,
    bool IsLoginPageLogo,
    bool IsActive,
    string TimeZoneId,
    TimeSpan BusinessStartTime,
    TimeSpan BusinessEndTime,
    bool WorksOnMonday,
    bool WorksOnTuesday,
    bool WorksOnWednesday,
    bool WorksOnThursday,
    bool WorksOnFriday,
    bool WorksOnSaturday,
    bool WorksOnSunday,
    IFormFile? Logo) : IRequest<int>;
