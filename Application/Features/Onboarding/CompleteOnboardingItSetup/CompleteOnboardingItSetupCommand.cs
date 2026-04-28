// Project comment: CompleteOnboardingItSetupCommand.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup
{
    /// <summary>
    /// Command used by IT admins to save a draft checklist or complete onboarding IT setup.
    /// </summary>
    public sealed class CompleteOnboardingItSetupCommand : IRequest<CompleteOnboardingItSetupResult>
    {
        /// <summary>
        /// Current onboarding page session, including selected assets, software, and request id.
        /// </summary>
        public OnboardingViewModel Session { get; set; } = new();

        /// <summary>
        /// When true, saves the checklist as a draft instead of completing the request.
        /// </summary>
        public bool IsDraft { get; set; }

        /// <summary>
        /// Confirms that the physical device cleanup or preparation check is complete.
        /// </summary>
        public bool CheckPhysicalClean { get; set; }

        /// <summary>
        /// Confirms Microsoft 365 setup is complete.
        /// </summary>
        public bool CheckO365Setup { get; set; }

        /// <summary>
        /// Confirms Windows login setup has been completed.
        /// </summary>
        public bool CheckWindowsLogin { get; set; }

        /// <summary>
        /// Confirms email account setup is complete.
        /// </summary>
        public bool CheckEmailSetup { get; set; }

        /// <summary>
        /// Confirms requested specific software has been prepared.
        /// </summary>
        public bool CheckSpecificSoftware { get; set; }

        /// <summary>
        /// Confirms standard or general software has been prepared.
        /// </summary>
        public bool CheckGeneralSoftware { get; set; }

        /// <summary>
        /// Confirms ABC group access has been configured.
        /// </summary>
        public bool CheckAbcGroup { get; set; }

        /// <summary>
        /// Confirms email distribution group membership has been configured.
        /// </summary>
        public bool CheckEmailMailGroup { get; set; }

        /// <summary>
        /// Confirms the device has been joined to the domain.
        /// </summary>
        public bool CheckDomainJoin { get; set; }

        /// <summary>
        /// Confirms shared drive access has been configured.
        /// </summary>
        public bool CheckSharedDrive { get; set; }

        /// <summary>
        /// Confirms Wi-Fi setup has been completed.
        /// </summary>
        public bool CheckWiFiSetup { get; set; }

        /// <summary>
        /// Confirms printer setup has been completed.
        /// </summary>
        public bool CheckPrinterSetup { get; set; }

        /// <summary>
        /// Confirms scan folder setup has been completed.
        /// </summary>
        public bool CheckScanFolder { get; set; }

        /// <summary>
        /// Confirms VPN setup has been completed.
        /// </summary>
        public bool CheckVPNSetup { get; set; }

        /// <summary>
        /// Optional IT remarks saved with the checklist.
        /// </summary>
        public string? ChecklistRemarks { get; set; }
    }
}
