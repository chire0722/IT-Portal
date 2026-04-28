// Project comment: GetOnboardingItStep5QueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Models;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep5
{
    /// <summary>
    /// Handles the GetOnboardingItStep5Query query workflow.
    /// </summary>
    public sealed class GetOnboardingItStep5QueryHandler : IRequestHandler<GetOnboardingItStep5Query, GetOnboardingItStep5Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetOnboardingItStep5QueryHandler(
            UserManager<ApplicationUser> userManager,
            ICurrentUserAccessor currentUserAccessor)
        {
            _userManager = userManager;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<GetOnboardingItStep5Result> Handle(GetOnboardingItStep5Query request, CancellationToken cancellationToken)
        {
            var model = request.SessionModel;
            if (model == null || model.Id == 0)
            {
                return new GetOnboardingItStep5Result(
                    GetOnboardingItStep5Status.SessionExpired,
                    null,
                    null,
                    "Session expired. Please use 'Continue IT Setup' from the request details page.",
                    0);
            }

            model.CurrentStep = 5;
            var user = await _userManager.GetUserAsync(_currentUserAccessor.User);

            return new GetOnboardingItStep5Result(
                GetOnboardingItStep5Status.Ready,
                model,
                MapToViewModel(model, model.Id, user?.Signature),
                null,
                model.Id);
        }

        /// <summary>
        /// Maps data for the MapToViewModel operation.
        /// </summary>
        private static OnboardingStep5PageViewModel MapToViewModel(
            OnboardingViewModel model,
            int requestId,
            string? currentUserSignature)
        {
            return new OnboardingStep5PageViewModel
            {
                Id = model.Id,
                CurrentStep = model.CurrentStep,
                FullName = model.FullName,
                EmployeeID = model.EmployeeID,
                Designation = model.Designation,
                Department = model.Department,
                Branch = model.Branch,
                DeskNumber = model.DeskNumber,
                JoinDate = model.JoinDate,
                EmailAddress = model.EmailAddress,
                ContactNumber = model.ContactNumber,
                HODEmail = model.HODEmail,
                IssuedAssets = model.IssuedAssets,
                SelectedStandardSoftwareIds = model.SelectedStandardSoftwareIds,
                SelectedSoftwareIds = model.SelectedSoftwareIds,
                SelectedEmailGroupIds = model.SelectedEmailGroupIds,
                OtherGroup = model.OtherGroup,
                OtherGroupDetails = model.OtherGroupDetails,
                SelectedSharedMailboxIds = model.SelectedSharedMailboxIds,
                HasOtherSharedMailbox = model.HasOtherSharedMailbox,
                OtherSharedMailboxDetails = model.OtherSharedMailboxDetails,
                AssetRemarks = model.AssetRemarks,
                SoftwareRemarks = model.SoftwareRemarks,
                FolderRemarks = model.FolderRemarks,
                OtherBranchDetails = model.OtherBranchDetails,
                OtherDepartmentDetails = model.OtherDepartmentDetails,
                HasOtherFolder = model.HasOtherFolder,
                OtherFolderDetails = model.OtherFolderDetails,
                FolderPermissions = model.FolderPermissions,
                RequesterSignature = model.RequesterSignature,
                ITAdminSignature = model.ITAdminSignature,
                CheckPhysicalClean = model.CheckPhysicalClean,
                CheckO365Setup = model.CheckO365Setup,
                CheckWindowsLogin = model.CheckWindowsLogin,
                CheckEmailSetup = model.CheckEmailSetup,
                CheckSpecificSoftware = model.CheckSpecificSoftware,
                CheckGeneralSoftware = model.CheckGeneralSoftware,
                CheckAbcGroup = model.CheckAbcGroup,
                CheckEmailMailGroup = model.CheckEmailMailGroup,
                CheckDomainJoin = model.CheckDomainJoin,
                CheckSharedDrive = model.CheckSharedDrive,
                CheckWiFiSetup = model.CheckWiFiSetup,
                CheckPrinterSetup = model.CheckPrinterSetup,
                CheckScanFolder = model.CheckScanFolder,
                CheckVPNSetup = model.CheckVPNSetup,
                ChecklistRemarks = model.ChecklistRemarks,
                RequestId = requestId,
                CurrentUserSignature = currentUserSignature
            };
        }
    }
}
