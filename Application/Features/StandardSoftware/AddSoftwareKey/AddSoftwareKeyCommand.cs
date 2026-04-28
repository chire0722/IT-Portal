// Project comment: AddSoftwareKeyCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareKey
{
    /// <summary>
    /// Carries data needed to execute the AddSoftwareKey command.
    /// </summary>
    public sealed record AddSoftwareKeyCommand(
        int SoftwareId,
        string KeyValue) : IRequest<AddSoftwareKeyResult>;
}
