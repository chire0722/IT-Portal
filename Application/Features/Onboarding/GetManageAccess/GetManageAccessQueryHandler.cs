// Project comment: GetManageAccessQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Services;
using IT_Form.ViewModels;
using Mapster;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetManageAccess
{
    /// <summary>
    /// Handles the GetManageAccessQuery query workflow.
    /// </summary>
    public sealed class GetManageAccessQueryHandler : IRequestHandler<GetManageAccessQuery, ManageAccessViewModel?>
    {
        private readonly ICurrentEmployeeAccessService _accessService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetManageAccessQueryHandler(
            ICurrentEmployeeAccessService accessService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _accessService = accessService;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ManageAccessViewModel?> Handle(GetManageAccessQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUserAccessor.User.HasClaim("UserTitle", "IT"))
            {
                return null;
            }

            var context = await _accessService.GetEmployeeContextAsync(request.Id, cancellationToken);
            if (context == null)
            {
                return null;
            }

            var onboardingRequest = context.Request;
            var accessCatalog = await _accessService.GetAccessCatalogAsync(
                context.CompanyId,
                onboardingRequest.RequestEmailGroups
                    .Where(group => group.EmailGroupId > 0)
                    .Select(group => group.EmailGroupId),
                onboardingRequest.RequestSharedMailboxes
                    .Where(mailbox => mailbox.SharedMailboxId > 0)
                    .Select(mailbox => mailbox.SharedMailboxId),
                onboardingRequest.FolderPermissions,
                cancellationToken);

            var viewModel = onboardingRequest.Adapt<ManageAccessViewModel>();
            viewModel.CurrentDepartment = context.CurrentDepartment;
            viewModel.CurrentBranch = context.CurrentBranch;
            viewModel.FolderPermissions = _accessService.BuildFolderPermissions(
                accessCatalog.ServerFolders,
                onboardingRequest.FolderPermissions);
            viewModel.AvailableEmailGroups = accessCatalog.EmailGroups;
            viewModel.AvailableSharedMailboxes = accessCatalog.SharedMailboxes;
            return viewModel;
        }
    }
}
