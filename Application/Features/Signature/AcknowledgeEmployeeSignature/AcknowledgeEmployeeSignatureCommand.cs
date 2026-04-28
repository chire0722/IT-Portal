// Project comment: AcknowledgeEmployeeSignatureCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Signature.AcknowledgeEmployeeSignature
{
    /// <summary>
    /// Carries data needed to execute the AcknowledgeEmployeeSignature command.
    /// </summary>
    public sealed record AcknowledgeEmployeeSignatureCommand(
        Guid Token,
        string Type,
        string SignatureData,
        bool PdpaConsent) : IRequest<ProcessResult<object>>;
}
