using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareLicense
{
    public sealed record AddSoftwareLicenseCommand(
        int StandardSoftwareId,
        string? SoftwareName,
        string? Description,
        bool IsSubscription,
        bool IsFree,
        DateTime? ExpiryDate,
        int? ContractLimit,
        string? VendorName,
        string? SupportContact) : IRequest<AddSoftwareLicenseResult>;
}
