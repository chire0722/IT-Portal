using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    public sealed record ProcessOffboardingHrSignatureCommand(int Id, string SignatureBase64) : IRequest<CommandResponse>;
}
