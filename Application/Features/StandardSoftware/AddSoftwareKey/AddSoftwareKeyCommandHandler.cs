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
        private readonly IT_Form.Data.ApplicationDbContext _context;
        private readonly IT_Form.Application.Common.Abstractions.ICurrentUserAccessor _currentUserAccessor;

        public AddSoftwareKeyCommandHandler(
            ILicenseService licenseService,
            IT_Form.Data.ApplicationDbContext context,
            IT_Form.Application.Common.Abstractions.ICurrentUserAccessor currentUserAccessor)
        {
            _licenseService = licenseService;
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<AddSoftwareKeyResult> Handle(AddSoftwareKeyCommand request, CancellationToken cancellationToken)
        {
            if (!await StandardSoftwareLicenseScopeSupport.CanAccessAsync(
                    _context, _currentUserAccessor.User, request.SoftwareId, cancellationToken))
            {
                return new AddSoftwareKeyResult(AddSoftwareKeyStatus.Failed, "Software version not found.");
            }

            var rawKeys = new List<string> { request.KeyValue };
            var result = await _licenseService.ImportKeysAsync(request.SoftwareId, rawKeys);
            return result.Success
                ? new AddSoftwareKeyResult(AddSoftwareKeyStatus.Added, result.Message)
                : new AddSoftwareKeyResult(AddSoftwareKeyStatus.Failed, result.Message);
        }
    }
}
