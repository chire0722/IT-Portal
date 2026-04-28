// Project comment: DeleteSoftwareLicenseCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense
{
    /// <summary>
    /// Carries data needed to execute the DeleteSoftwareLicense command.
    /// </summary>
    public sealed record DeleteSoftwareLicenseCommand(int Id) : IRequest<DeleteSoftwareLicenseResult>;
}
