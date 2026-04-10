using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.ITAsset.CreateItAsset
{
    public sealed record CreateItAssetCommand(
        IT_Form.Models.ITAsset Asset,
        int? AssignedEmployeeId,
        string? CustomEmployeeName) : IRequest<CommandResponse>;
}
