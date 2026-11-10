// Project comment: CreateItAssetCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ITAsset.CreateItAsset
{
    /// <summary>
    /// Handles the CreateItAssetCommand command workflow.
    /// </summary>
    public sealed class CreateItAssetCommandHandler : IRequestHandler<CreateItAssetCommand, CommandResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IQuotaControl _quotaControl;

        public CreateItAssetCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor, IQuotaControl? quotaControl = null)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _quotaControl = quotaControl ?? new QuotaControl(context);
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(CreateItAssetCommand request, CancellationToken cancellationToken)
        {
            await _quotaControl.EnsureAvailableAsync(QuotaControl.Assets, user: _currentUserAccessor.User, cancellationToken: cancellationToken);

            if (!ItAdminCompanyScopeSupport.CanAccessCompany(_currentUserAccessor.User, request.Asset.Company))
            {
                return new CommandResponse
                {
                    IsForbidden = true,
                    Message = "You are not authorized to create this asset."
                };
            }

            if (request.Asset.Status == AssetStatus.Issued)
            {
                if (!request.AssignedEmployeeId.HasValue)
                {
                    return new CommandResponse { Message = "An issued asset must have a directory employee assignment." };
                }

                var assignment = await _context.OnboardingRequests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == request.AssignedEmployeeId.Value, cancellationToken);
                if (assignment == null ||
                    (assignment.Status != RequestStatus.Completed &&
                     assignment.Status != RequestStatus.PendingEmployeeSign &&
                     assignment.Status != RequestStatus.Approved) ||
                    !string.Equals(assignment.Branch, request.Asset.Company, StringComparison.OrdinalIgnoreCase))
                {
                    return new CommandResponse { Message = "The asset assignment must target an active employee in the same company." };
                }
            }

            if (request.Asset.Status != AssetStatus.Issued &&
                (request.AssignedEmployeeId.HasValue || !string.IsNullOrWhiteSpace(request.CustomEmployeeName)))
            {
                return new CommandResponse { Message = "Only issued assets may have an employee assignment." };
            }

            _context.ITAssets.Add(request.Asset);

            if (request.Asset.Status == AssetStatus.Issued && request.AssignedEmployeeId.HasValue)
            {
                _context.IssuedAssets.Add(new IssuedAsset
                {
                    OnboardingRequestId = request.AssignedEmployeeId,
                    ITAsset = request.Asset,
                    CustomEmployeeName = request.CustomEmployeeName,
                    AssetType = request.Asset.AssetType,
                    OtherAssetType = request.Asset.OtherAssetType,
                    SerialNumber = request.Asset.SerialNumber,
                    AssetTag = request.Asset.AssetTag,
                    Qty = 1,
                    BrandModel = request.Asset.Model
                });

            }

            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse
            {
                IsSuccess = true,
                Message = "IT asset created."
            };
        }
    }
}

