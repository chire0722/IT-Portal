// Project comment: CreateItAssetCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.ITAsset.CreateItAsset
{
    /// <summary>
    /// Carries data needed to execute the CreateItAsset command.
    /// </summary>
    public sealed record CreateItAssetCommand(
        IT_Form.Models.ITAsset Asset,
        int? AssignedEmployeeId,
        string? CustomEmployeeName) : IRequest<CommandResponse>;
}
