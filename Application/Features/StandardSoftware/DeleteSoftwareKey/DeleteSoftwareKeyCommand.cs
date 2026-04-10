using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareKey
{
    public sealed record DeleteSoftwareKeyCommand(int Id) : IRequest<DeleteSoftwareKeyResult>;
}
