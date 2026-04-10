using IT_Form.Application.Common.Models;
using System.Security.Claims;

namespace IT_Form.Services
{
    public interface IOnboardingDocumentService
    {
        Task<ProcessResult<BinaryFilePayload>> ExportBulkPdfAsync(int[] ids, ClaimsPrincipal user, CancellationToken cancellationToken);
        Task<ProcessResult<BinaryFilePayload>> GeneratePdfAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken);
    }
}
