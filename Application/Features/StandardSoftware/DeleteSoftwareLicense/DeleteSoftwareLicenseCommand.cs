using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense
{
    public sealed record DeleteSoftwareLicenseCommand(int Id) : IRequest<DeleteSoftwareLicenseResult>;
}
