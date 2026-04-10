using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.Create
{
    public sealed class SubmitOffboardingCreateCommandHandler
        : IRequestHandler<SubmitOffboardingCreateCommand, ProcessResult<OffboardingCreatePageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public SubmitOffboardingCreateCommandHandler(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _env = env;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<OffboardingCreatePageViewModel>> Handle(
            SubmitOffboardingCreateCommand command,
            CancellationToken cancellationToken)
        {
            var request = command.Request;
            request.CreatedByUserId = _currentUserAccessor.UserName;
            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, request.Branch))
            {
                return ProcessResult<OffboardingCreatePageViewModel>.Forbidden();
            }

            var hodValidation = await OffboardingCreateSupport.ValidateAssignedHodAsync(_context, request, cancellationToken);
            if (hodValidation != null)
            {
                return await OffboardingCreateSupport.FailureAsync(_context, _currentUserAccessor, hodValidation, request, cancellationToken);
            }

            var fileValidation = OffboardingCreateSupport.ValidateResignationLetter(command.ResignationLetter);
            if (fileValidation != null)
            {
                return await OffboardingCreateSupport.FailureAsync(_context, _currentUserAccessor, fileValidation, request, cancellationToken);
            }

            var hasActiveRequest = await _context.OffboardingRequests
                .AnyAsync(r => r.OnboardingRequestId == request.OnboardingRequestId && r.Status != RequestStatus.Rejected, cancellationToken);

            if (hasActiveRequest)
            {
                return await OffboardingCreateSupport.FailureAsync(
                    _context,
                    _currentUserAccessor,
                    "An active offboarding request already exists for this employee.",
                    request,
                    cancellationToken);
            }

            _context.OffboardingRequests.Add(request);
            await _context.SaveChangesAsync(cancellationToken);

            await OffboardingCreateSupport.SaveResignationLetterAsync(_context, _env, request, command.ResignationLetter, cancellationToken);

            return ProcessResult<OffboardingCreatePageViewModel>.Success(
                message: "Offboarding workflow initiated successfully.",
                redirectAction: "Details",
                requestId: request.Id);
        }
    }
}
