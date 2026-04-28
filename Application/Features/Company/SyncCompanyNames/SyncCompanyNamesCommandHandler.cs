// Project comment: SyncCompanyNamesCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Services;
using MediatR;

namespace IT_Form.Application.Features.Company.SyncCompanyNames
{
    /// <summary>
    /// Handles the SyncCompanyNamesCommand command workflow.
    /// </summary>
    public sealed class SyncCompanyNamesCommandHandler : IRequestHandler<SyncCompanyNamesCommand, SyncCompanyNamesResult>
    {
        private readonly ICompanyReferenceSyncService _companyReferenceSyncService;

        public SyncCompanyNamesCommandHandler(ICompanyReferenceSyncService companyReferenceSyncService)
        {
            _companyReferenceSyncService = companyReferenceSyncService;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<SyncCompanyNamesResult> Handle(SyncCompanyNamesCommand request, CancellationToken cancellationToken)
        {
            var syncResult = await _companyReferenceSyncService.SyncAllActiveCompaniesAsync();
            var message = $"Synchronized {syncResult.UpdatedCount} company reference record(s).";
            if (syncResult.AmbiguousReferences.Any())
            {
                message += " Skipped ambiguous references: " + string.Join(", ", syncResult.AmbiguousReferences) + ".";
            }

            return new SyncCompanyNamesResult(message);
        }
    }
}
