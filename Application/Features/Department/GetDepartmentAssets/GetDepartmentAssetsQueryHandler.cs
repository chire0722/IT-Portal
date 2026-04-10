using IT_Form.Data;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Department.GetDepartmentAssets;

public sealed class GetDepartmentAssetsQueryHandler
    : IRequestHandler<GetDepartmentAssetsQuery, List<DepartmentAssetDto>>
{
    private readonly ApplicationDbContext _context;

    public GetDepartmentAssetsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentAssetDto>> Handle(
        GetDepartmentAssetsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.IssuedAssets
            .AsNoTracking()
            .Where(issuedAsset => issuedAsset.ITAsset != null)
            .Where(issuedAsset => issuedAsset.ITAssetId.HasValue)
            .Where(issuedAsset =>
                issuedAsset.Id == _context.IssuedAssets
                    .Where(currentAsset => currentAsset.ITAssetId == issuedAsset.ITAssetId)
                    .Max(currentAsset => currentAsset.Id))
            .Where(issuedAsset =>
                issuedAsset.OnboardingRequest != null &&
                issuedAsset.OnboardingRequest.Department == request.DepartmentName)
            .OrderBy(issuedAsset => issuedAsset.OnboardingRequest!.FullName)
            .ThenBy(issuedAsset => issuedAsset.ITAsset!.AssetTag)
            .ProjectToType<DepartmentAssetDto>()
            .ToListAsync(cancellationToken);
    }
}
