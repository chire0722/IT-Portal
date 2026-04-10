using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.RegenerateOffboardingToken
{
    public sealed class RegenerateOffboardingTokenCommandHandler : IRequestHandler<RegenerateOffboardingTokenCommand, ProcessResult<object>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public RegenerateOffboardingTokenCommandHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<object>> Handle(RegenerateOffboardingTokenCommand request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (!user.HasClaim("UserTitle", "HR"))
            {
                return ProcessResult<object>.Forbidden();
            }

            var offboardingRequest = await _context.OffboardingRequests
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (offboardingRequest == null)
            {
                return ProcessResult<object>.NotFound();
            }

            if (!offboardingRequest.CanRegenerateToken())
            {
                return ProcessResult<object>.Failure("Token can only be regenerated for requests awaiting employee signature.", "Details", offboardingRequest.Id, ajaxAware: true);
            }

            offboardingRequest.RegenerateAccessToken(DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);

            return new ProcessResult<object>
            {
                IsSuccess = true,
                Message = "Signature link has been regenerated. The old link is now invalid.",
                RedirectAction = "Details",
                RequestId = offboardingRequest.Id,
                AjaxAware = true
            };
        }
    }
}
