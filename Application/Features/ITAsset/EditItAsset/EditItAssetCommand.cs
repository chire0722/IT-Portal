using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.ITAsset.EditItAsset
{
    public sealed record EditItAssetCommand(
        int Id,
        IT_Form.Models.ITAsset Asset,
        int? AssignedEmployeeId,
        string? CustomEmployeeName) : IRequest<CommandResponse>;
}
