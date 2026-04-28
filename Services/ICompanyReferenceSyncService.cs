// Project comment: ICompanyReferenceSyncService.cs is part of the IT Form application source code.

using IT_Form.Models;

namespace IT_Form.Services
{
    /// <summary>
    /// Represents the result returned by the CompanyReferenceSync workflow.
    /// </summary>
    public record CompanyReferenceSyncResult(int UpdatedCount, IReadOnlyCollection<string> AmbiguousReferences);

    /// <summary>
    /// Provides ICompanyReferenceSync service behavior for the application.
    /// </summary>
    public interface ICompanyReferenceSyncService
    {
        Task<int> ApplyUpdatedCompanySyncAsync(Company originalCompany, Company updatedCompany, CancellationToken cancellationToken = default);
        Task<CompanyReferenceSyncResult> SyncAllActiveCompaniesAsync(CancellationToken cancellationToken = default);
    }
}
