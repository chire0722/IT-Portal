// Project comment: EditItAssetCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.ITAsset.EditItAsset
{
    /// <summary>
    /// Carries data needed to execute the EditItAsset command.
    /// </summary>
    public sealed record EditItAssetCommand(
        int Id,
        IT_Form.Models.ITAsset Asset,
        int? AssignedEmployeeId,
        string? CustomEmployeeName) : IRequest<CommandResponse>;
}
