using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.UpdateSoftwareKey
{
    public sealed record UpdateSoftwareKeyCommand(
        int Id,
        string KeyValue) : IRequest<UpdateSoftwareKeyResult>;
}
