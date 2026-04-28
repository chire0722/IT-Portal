// Project comment: DeleteSoftwareKeyCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareKey
{
    /// <summary>
    /// Carries data needed to execute the DeleteSoftwareKey command.
    /// </summary>
    public sealed record DeleteSoftwareKeyCommand(int Id) : IRequest<DeleteSoftwareKeyResult>;
}
