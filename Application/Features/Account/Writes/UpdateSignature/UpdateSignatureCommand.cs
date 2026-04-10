using MediatR;

namespace IT_Form.Application.Features.Account.Writes.UpdateSignature;

public sealed record UpdateSignatureCommand(
    string UserId,
    string? SignatureData) : IRequest<Unit>;
