// Project comment: DeleteOffboardingRequestCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Hosting;
using IT_Form.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.DeleteOffboardingRequest
{
    /// <summary>
    /// Handles the DeleteOffboardingRequestCommand command workflow.
    /// </summary>
    public sealed class DeleteOffboardingRequestCommandHandler : IRequestHandler<DeleteOffboardingRequestCommand, CommandResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DeleteOffboardingRequestCommandHandler>? _logger;

        public DeleteOffboardingRequestCommandHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<DeleteOffboardingRequestCommandHandler>? logger = null)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(DeleteOffboardingRequestCommand request, CancellationToken cancellationToken)
        {
            var offboardingRequest = await _context.OffboardingRequests.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (offboardingRequest == null)
                return new CommandResponse { IsNotFound = true };

            if (!await ActionWrites.OffboardingActionWriteHelpers.IsUserAuthorizedForRequest(_context, _currentUserAccessor.User, _userManager, offboardingRequest, _currentUserAccessor.UserName))
                return new CommandResponse { IsForbidden = true };

            var isHr = _currentUserAccessor.User.HasClaim("UserTitle", "HR");
            if (!offboardingRequest.CanBeDeletedBy(isHr))
            {
                return new CommandResponse { Message = "Only Draft, Rejected, or HR-signed (awaiting others) requests can be deleted." };
            }

            await _context.LogActionAsync(nameof(OffboardingRequest), request.Id.ToString(), "Deleted",
                $"Offboarding form for {offboardingRequest.FullName} was deleted by {_currentUserAccessor.UserName}. Status was: {offboardingRequest.Status}");

            var storedFiles = new[]
            {
                offboardingRequest.ResignationLetterPath,
                offboardingRequest.HardwareDamageImagePath,
                offboardingRequest.HardwareReturnImagePath
            };

            _context.OffboardingRequests.Remove(offboardingRequest);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var storedPath in storedFiles.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.Ordinal))
            {
                try
                {
                    SensitiveFileStorage.DeleteStoredFile(_environment, storedPath);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    _logger?.LogError(ex, "Failed to delete a sensitive file after offboarding request {RequestId} was deleted. Manual cleanup is required.", request.Id);
                }
            }

            return new CommandResponse
            {
                IsSuccess = true,
                Message = $"Offboarding request for {offboardingRequest.FullName} has been deleted."
            };
        }
    }
}
