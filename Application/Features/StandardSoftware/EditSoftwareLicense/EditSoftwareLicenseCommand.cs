// Project comment: EditSoftwareLicenseCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.EditSoftwareLicense
{
    /// <summary>
    /// Carries data needed to execute the EditSoftwareLicense command.
    /// </summary>
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
