// Project comment: AddSoftwareLicenseCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareLicense
{
    /// <summary>
    /// Carries data needed to execute the AddSoftwareLicense command.
    /// </summary>
    public sealed record AddSoftwareLicenseCommand(
        int StandardSoftwareId,
        string? SoftwareName,
        string? Description,
        bool IsSubscription,
        bool IsFree,
        DateTime? ExpiryDate,
        int? ContractLimit,
        string? VendorName,
        string? SupportContact,
        int[] SelectedCompanyIds) : IRequest<AddSoftwareLicenseResult>;
}
