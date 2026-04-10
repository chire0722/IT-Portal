using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Signature.AcknowledgeEmployeeSignature
{
    public sealed record AcknowledgeEmployeeSignatureCommand(
        Guid Token,
        string Type,
        string SignatureData) : IRequest<ProcessResult<object>>;
}
