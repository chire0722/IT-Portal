// Project comment: SubmitOffboardingCreateCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.Create
{
    /// <summary>
    /// Handles the SubmitOffboardingCreateCommand command workflow.
    /// </summary>
    public sealed class SubmitOffboardingCreateCommandHandler
        : IRequestHandler<SubmitOffboardingCreateCommand, ProcessResult<OffboardingCreatePageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IFileMalwareScanner _malwareScanner;

        public SubmitOffboardingCreateCommandHandler(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ICurrentUserAccessor currentUserAccessor,
            IFileMalwareScanner malwareScanner)
        {
            _context = context;
            _env = env;
            _currentUserAccessor = currentUserAccessor;
            _malwareScanner = malwareScanner;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
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

            if (command.ResignationLetter is { Length: > 0 })
            {
                await using var uploadStream = command.ResignationLetter.OpenReadStream();
                var scanVerdict = await _malwareScanner.ScanAsync(uploadStream, cancellationToken);
                if (scanVerdict == MalwareScanVerdict.Infected)
                {
                    return await OffboardingCreateSupport.FailureAsync(
                        _context,
                        _currentUserAccessor,
                        "The uploaded file failed security scanning.",
                        request,
                        cancellationToken);
                }

                if (scanVerdict != MalwareScanVerdict.Clean)
                {
                    return await OffboardingCreateSupport.FailureAsync(
                        _context,
                        _currentUserAccessor,
                        "Upload scanning is temporarily unavailable. Please try again later.",
                        request,
                        cancellationToken);
                }
            }

            var hasActiveRequest = request.OnboardingRequestId.HasValue &&
                await _context.OffboardingRequests
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

