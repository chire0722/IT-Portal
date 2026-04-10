using IT_Form.Models;

namespace IT_Form.Services
{
    public record CompanyReferenceSyncResult(int UpdatedCount, IReadOnlyCollection<string> AmbiguousReferences);

    public interface ICompanyReferenceSyncService
    {
        Task<int> ApplyUpdatedCompanySyncAsync(Company originalCompany, Company updatedCompany, CancellationToken cancellationToken = default);
        Task<CompanyReferenceSyncResult> SyncAllActiveCompaniesAsync(CancellationToken cancellationToken = default);
    }
}
