using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;

namespace IT_Form.Application.Features.ITAsset.CreateItAsset
{
    public sealed class CreateItAssetCommandHandler : IRequestHandler<CreateItAssetCommand, CommandResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public CreateItAssetCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<CommandResponse> Handle(CreateItAssetCommand request, CancellationToken cancellationToken)
        {
            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, request.Asset.Company))
            {
                return new CommandResponse
                {
                    IsForbidden = true,
                    Message = "You can only create assets for your assigned company scope."
                };
            }

            _context.ITAssets.Add(request.Asset);
            await _context.SaveChangesAsync(cancellationToken);

            if (request.Asset.Status == AssetStatus.Issued && request.AssignedEmployeeId.HasValue)
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

                await _context.SaveChangesAsync(cancellationToken);
            }

            return new CommandResponse
            {
                IsSuccess = true,
                Message = "IT asset created."
            };
        }
    }
}
