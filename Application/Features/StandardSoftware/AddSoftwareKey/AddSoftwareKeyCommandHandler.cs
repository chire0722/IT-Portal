// Project comment: AddSoftwareKeyCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Services;
using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareKey
{
    /// <summary>
    /// Handles the AddSoftwareKeyCommand command workflow.
    /// </summary>
    public sealed class AddSoftwareKeyCommandHandler : IRequestHandler<AddSoftwareKeyCommand, AddSoftwareKeyResult>
    {
        private readonly ILicenseService _licenseService;

        public AddSoftwareKeyCommandHandler(ILicenseService licenseService)
        {
            _licenseService = licenseService;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
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
