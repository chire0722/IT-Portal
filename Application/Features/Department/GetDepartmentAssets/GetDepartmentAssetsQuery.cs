// Project comment: GetDepartmentAssetsQuery.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Department.GetDepartmentAssets;

/// <summary>
/// Carries data needed to execute the GetDepartmentAssets query.
/// </summary>
public sealed record GetDepartmentAssetsQuery(string DepartmentName) : IRequest<List<DepartmentAssetDto>>;
