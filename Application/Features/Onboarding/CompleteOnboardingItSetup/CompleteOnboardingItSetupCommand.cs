using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup
{
    public sealed class CompleteOnboardingItSetupCommand : IRequest<CompleteOnboardingItSetupResult>
    {
        public OnboardingViewModel Session { get; set; } = new();
        public bool IsDraft { get; set; }
        public bool CheckPhysicalClean { get; set; }
        public bool CheckO365Setup { get; set; }
        public bool CheckWindowsLogin { get; set; }
        public bool CheckEmailSetup { get; set; }
        public bool CheckSpecificSoftware { get; set; }
        public bool CheckGeneralSoftware { get; set; }
        public bool CheckAbcGroup { get; set; }
        public bool CheckEmailMailGroup { get; set; }
        public bool CheckDomainJoin { get; set; }
        public bool CheckSharedDrive { get; set; }
        public bool CheckWiFiSetup { get; set; }
        public bool CheckPrinterSetup { get; set; }
        public bool CheckScanFolder { get; set; }
        public bool CheckVPNSetup { get; set; }
        public string? ChecklistRemarks { get; set; }
    }
}
