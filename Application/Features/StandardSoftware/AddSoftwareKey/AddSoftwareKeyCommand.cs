using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareKey
{
    public sealed record AddSoftwareKeyCommand(
        int SoftwareId,
        string KeyValue) : IRequest<AddSoftwareKeyResult>;
}
