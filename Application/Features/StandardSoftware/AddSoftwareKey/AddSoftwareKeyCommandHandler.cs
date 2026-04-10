using IT_Form.Services;
using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareKey
{
    public sealed class AddSoftwareKeyCommandHandler : IRequestHandler<AddSoftwareKeyCommand, AddSoftwareKeyResult>
    {
        private readonly ILicenseService _licenseService;

        public AddSoftwareKeyCommandHandler(ILicenseService licenseService)
        {
            _licenseService = licenseService;
        }

        public async Task<AddSoftwareKeyResult> Handle(AddSoftwareKeyCommand request, CancellationToken cancellationToken)
        {
            var rawKeys = new List<string> { request.KeyValue };
            var result = await _licenseService.ImportKeysAsync(request.SoftwareId, rawKeys);
            return result.Success
                ? new AddSoftwareKeyResult(AddSoftwareKeyStatus.Added, result.Message)
                : new AddSoftwareKeyResult(AddSoftwareKeyStatus.Failed, result.Message);
        }
    }
}
