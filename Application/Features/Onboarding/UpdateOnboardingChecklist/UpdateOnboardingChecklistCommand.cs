// Project comment: UpdateOnboardingChecklistCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingChecklist
{
    /// <summary>
    /// Carries data needed to execute the UpdateOnboardingChecklist command.
    /// </summary>
    public sealed record UpdateOnboardingChecklistCommand(
        int Id,
        bool CheckPhysicalClean,
        bool CheckO365Setup,
        bool CheckWindowsLogin,
        bool CheckEmailSetup,
        bool CheckSpecificSoftware,
        bool CheckGeneralSoftware,
        bool CheckAbcGroup,
        bool CheckEmailMailGroup,
        bool CheckDomainJoin,
        bool CheckSharedDrive,
        bool CheckWiFiSetup,
        bool CheckPrinterSetup,
        bool CheckScanFolder,
        bool CheckVPNSetup,
        string? ChecklistRemarks) : IRequest<ProcessResult<object>>;
}
