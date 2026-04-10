using MediatR;

namespace IT_Form.Application.Features.Company.SyncCompanyNames
{
    public sealed record SyncCompanyNamesCommand : IRequest<SyncCompanyNamesResult>;
}
