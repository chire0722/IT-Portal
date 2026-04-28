// Project comment: SyncCompanyNamesCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Company.SyncCompanyNames
{
    /// <summary>
    /// Carries data needed to execute the SyncCompanyNames command.
    /// </summary>
    public sealed record SyncCompanyNamesCommand : IRequest<SyncCompanyNamesResult>;
}
