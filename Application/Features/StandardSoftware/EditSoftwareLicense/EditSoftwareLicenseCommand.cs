using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.EditSoftwareLicense
{
    public sealed record EditSoftwareLicenseCommand(
        int Id,
        string SoftwareName,
        string? Description,
        bool IsSubscription,
        bool IsFree,
        DateTime? ExpiryDate,
        int? ContractLimit,
        string? VendorName,
        string? SupportContact,
        int[] SelectedCompanyIds) : IRequest<EditSoftwareLicenseResult>;
}
