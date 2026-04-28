// Project comment: UpdateSoftwareKeyCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.UpdateSoftwareKey
{
    /// <summary>
    /// Carries data needed to execute the UpdateSoftwareKey command.
    /// </summary>
    public sealed record UpdateSoftwareKeyCommand(
        int Id,
        string KeyValue) : IRequest<UpdateSoftwareKeyResult>;
}
