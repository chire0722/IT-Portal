using MediatR;

namespace IT_Form.Application.Features.Department.GetDepartmentAssets;

public sealed record GetDepartmentAssetsQuery(string DepartmentName) : IRequest<List<DepartmentAssetDto>>;
