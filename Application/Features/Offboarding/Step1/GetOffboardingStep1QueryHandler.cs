using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Offboarding.ActionWrites;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.Step1
{
    public sealed class GetOffboardingStep1QueryHandler : IRequestHandler<GetOffboardingStep1Query, ProcessResult<OffboardingStep1PageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetOffboardingStep1QueryHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _userManager = userManager;
        }

        public async Task<ProcessResult<OffboardingStep1PageViewModel>> Handle(GetOffboardingStep1Query request, CancellationToken cancellationToken)
        {
            var offboardingRequest = await _context.OffboardingRequests
                .Include(o => o.OnboardingRequest)
                    .ThenInclude(or => or!.IssuedAssets)
                .Include(o => o.OnboardingRequest)
                    .ThenInclude(or => or!.FolderPermissions)
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (offboardingRequest == null)
            {
                return ProcessResult<OffboardingStep1PageViewModel>.NotFound();
            }

            if (!await OffboardingActionWriteHelpers.IsUserAuthorizedForRequest(
                    _context,
                    _currentUserAccessor.User,
                    _userManager,
                    offboardingRequest,
                    _currentUserAccessor.UserName))
            {
                return ProcessResult<OffboardingStep1PageViewModel>.Forbidden();
            }

            if (offboardingRequest.Status == RequestStatus.PendingEmployeeSign ||
                offboardingRequest.Status == RequestStatus.Completed ||
                offboardingRequest.Status == RequestStatus.Rejected)
            {
                return ProcessResult<OffboardingStep1PageViewModel>.Failure(
                    "This request cannot be edited in its current status.",
                    "Details",
                    request.Id);
            }

            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .Select(c => string.IsNullOrEmpty(c.Lot) ? c.Name : $"{c.Name} ({c.Lot})")
                .ToListAsync(cancellationToken);

            return ProcessResult<OffboardingStep1PageViewModel>.Success(new OffboardingStep1PageViewModel(offboardingRequest)
            {
                Companies = companies
            });
        }
    }
}
