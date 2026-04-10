using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    public sealed class ProcessOffboardingItSignatureCommandHandler : IRequestHandler<ProcessOffboardingItSignatureCommand, CommandResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISignatureIntegrityService _integrityService;

        public ProcessOffboardingItSignatureCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor, UserManager<ApplicationUser> userManager, ISignatureIntegrityService integrityService)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
            _integrityService = integrityService;
        }

        public async Task<CommandResponse> Handle(ProcessOffboardingItSignatureCommand request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "IT"))
                return new CommandResponse { Message = "Only users with IT title can sign as IT Admin.", RequestId = request.Id };

            var offboardingRequest = await _context.OffboardingRequests.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (offboardingRequest == null)
                return new CommandResponse { IsNotFound = true };

            var currentUser = await _userManager.GetUserAsync(user);
            var signedAt = DateTime.UtcNow;
            var signerName = !string.IsNullOrWhiteSpace(currentUser?.FullName) ? currentUser.FullName : (_currentUserAccessor.UserName ?? "Unknown");
            var signatureHmac = _integrityService.ComputeHmac("OffboardingRequest", offboardingRequest.Id.ToString(), signerName, signedAt, request.SignatureBase64);

            offboardingRequest.SignByIt(request.SignatureBase64, signerName, signatureHmac, signedAt);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse { IsSuccess = true, Message = "IT Admin signature saved successfully.", RequestId = offboardingRequest.Id };
        }
    }
}
