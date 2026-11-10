// Project comment: EditItAssetCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ITAsset.EditItAsset
{
    /// <summary>
    /// Handles the EditItAssetCommand command workflow.
    /// </summary>
    public sealed class EditItAssetCommandHandler : IRequestHandler<EditItAssetCommand, CommandResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public EditItAssetCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(EditItAssetCommand request, CancellationToken cancellationToken)
        {
            var existingAsset = await _context.ITAssets
                .AsNoTracking()
                .FirstOrDefaultAsync(asset => asset.Id == request.Id, cancellationToken);

            if (existingAsset == null)
            {
                return new CommandResponse
                {
                    IsNotFound = true,
                    Message = "IT asset not found."
                };
            }

            if (!ItAdminCompanyScopeSupport.CanAccessCompany(_currentUserAccessor.User, existingAsset.Company) ||
                !ItAdminCompanyScopeSupport.CanAccessCompany(_currentUserAccessor.User, request.Asset.Company))
            {
                return new CommandResponse
                {
                    IsForbidden = true,
                    Message = "You are not authorized to modify this asset."
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

            if (existingAsset.WarrantyExpiry != request.Asset.WarrantyExpiry)
            {
                request.Asset.WarrantyNotifyStatus = ExpiryNotifyStatus.Pending;
            }
            else
            {
                request.Asset.WarrantyNotifyStatus = existingAsset.WarrantyNotifyStatus;
            }

            request.Asset.CPU = existingAsset.CPU;
            request.Asset.LastSeen = existingAsset.LastSeen;
            request.Asset.HardwareHash = existingAsset.HardwareHash;
            request.Asset.HardwareAuditJson = existingAsset.HardwareAuditJson;
            request.Asset.LatestHardwareHash = existingAsset.LatestHardwareHash;
            request.Asset.LatestHardwareAuditJson = existingAsset.LatestHardwareAuditJson;
            request.Asset.HasHardwareFingerprintMismatch = existingAsset.HasHardwareFingerprintMismatch;
            request.Asset.HardwareFingerprintMismatchDetectedAt = existingAsset.HardwareFingerprintMismatchDetectedAt;
            request.Asset.HardwareFingerprintWarning = existingAsset.HardwareFingerprintWarning;
            request.Asset.ExpiryAttemptCount = existingAsset.ExpiryAttemptCount;
            request.Asset.IsDeleted = existingAsset.IsDeleted;
            request.Asset.DeletedAt = existingAsset.DeletedAt;
            request.Asset.DeletedByUserId = existingAsset.DeletedByUserId;

            if (request.Asset.Status == AssetStatus.Issued &&
                (request.AssignedEmployeeId.HasValue || !string.IsNullOrWhiteSpace(request.CustomEmployeeName)))
            {
                var existingIssue = await _context.IssuedAssets
                    .OrderByDescending(issuedAsset => issuedAsset.Id)
                    .FirstOrDefaultAsync(issuedAsset => issuedAsset.ITAssetId == request.Asset.Id, cancellationToken);

                var isDifferentAssignment = true;
                if (existingIssue != null)
                {
                    if (request.AssignedEmployeeId.HasValue &&
                        existingIssue.OnboardingRequestId == request.AssignedEmployeeId.Value)
                    {
                        isDifferentAssignment = false;
                    }
                    else if (!string.IsNullOrWhiteSpace(request.CustomEmployeeName) &&
                             existingIssue.CustomEmployeeName == request.CustomEmployeeName)
                    {
                        isDifferentAssignment = false;
                    }
                }

                if (isDifferentAssignment)
                {
                    _context.IssuedAssets.Add(new IssuedAsset
                    {
                        OnboardingRequestId = request.AssignedEmployeeId,
                        CustomEmployeeName = request.CustomEmployeeName,
                        ITAssetId = request.Asset.Id,
                        AssetType = request.Asset.AssetType,
                        OtherAssetType = request.Asset.OtherAssetType,
                        SerialNumber = request.Asset.SerialNumber,
                        AssetTag = request.Asset.AssetTag,
                        Qty = 1,
                        BrandModel = request.Asset.Model
                    });

                }
            }

            _context.ITAssets.Update(request.Asset);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse
            {
                IsSuccess = true,
                Message = "IT asset updated."
            };
        }
    }
}

