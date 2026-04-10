using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingEdit
{
    public sealed class GetOnboardingEditQueryHandler : IRequestHandler<GetOnboardingEditQuery, ProcessResult<EditOnboardingPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IOnboardingHodService _onboardingHodService;

        public GetOnboardingEditQueryHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            IOnboardingHodService onboardingHodService)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _onboardingHodService = onboardingHodService;
        }

        public async Task<ProcessResult<EditOnboardingPageViewModel>> Handle(GetOnboardingEditQuery request, CancellationToken cancellationToken)
        {
            var onboardingRequest = await _context.OnboardingRequests
                .AsNoTracking()
                .Include(r => r.IssuedAssets)
                .Include(r => r.FolderPermissions)
                .Include(r => r.RequestSoftware)
                .Include(r => r.RequestEmailGroups)
                .Include(r => r.RequestSharedMailboxes)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (onboardingRequest == null)
            {
                return ProcessResult<EditOnboardingPageViewModel>.NotFound();
            }

            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, onboardingRequest.Branch))
            {
                return ProcessResult<EditOnboardingPageViewModel>.Forbidden();
            }

            if (!onboardingRequest.CanBeEdited())
            {
                return ProcessResult<EditOnboardingPageViewModel>.Failure(
                    "This request is locked and cannot be edited because it has already been signed or completed.",
                    "Details",
                    request.Id);
            }

            if (onboardingRequest.Status == RequestStatus.Draft)
            {
                return ProcessResult<EditOnboardingPageViewModel>.Success();
            }

            var branchName = onboardingRequest.Branch;
            var editCompanyId = !string.IsNullOrEmpty(branchName)
                ? await _context.Companies
                    .AsNoTracking()
                    .Where(c => c.Name == branchName)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                : (int?)null;

            var standardSoftware = await _context.StandardSoftware
                .AsNoTracking()
                .Where(s => s.IsActive)
                .ToListAsync(cancellationToken);
            var emailGroups = await _context.EmailGroups
                .AsNoTracking()
                .Include(g => g.EmailGroupCompanies)
                .Where(g => g.IsActive && (!g.EmailGroupCompanies.Any() || g.EmailGroupCompanies.Any(c => c.CompanyId == editCompanyId)))
                .ToListAsync(cancellationToken);
            var sharedMailboxes = await _context.SharedMailboxes
                .AsNoTracking()
                .Include(m => m.SharedMailboxCompanies)
                .Where(m => m.IsActive && (!m.SharedMailboxCompanies.Any() || m.SharedMailboxCompanies.Any(c => c.CompanyId == editCompanyId)))
                .ToListAsync(cancellationToken);
            var companies = await CompanyScopeHelper.LoadAccessibleCompanyDisplayNamesAsync(_context, _currentUserAccessor.User, cancellationToken);
            var accessibleCompanyIds = await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken, activeOnly: true);
            var departments = await _context.Departments
                .AsNoTracking()
                .Where(d => d.IsActive)
                .Include(d => d.DepartmentCompanies)
                    .ThenInclude(dc => dc.Company)
                .Where(d => accessibleCompanyIds.Count == 0 || d.DepartmentCompanies.Any(dc => accessibleCompanyIds.Contains(dc.CompanyId)))
                .ToListAsync(cancellationToken);
            var hodUsers = (await _onboardingHodService.BuildHodSelectListAsync(
                onboardingRequest.Branch,
                onboardingRequest.HODEmail,
                cancellationToken))
                .Items.Cast<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                .ToList();

            return ProcessResult<EditOnboardingPageViewModel>.Success(
                MapToViewModel(onboardingRequest, standardSoftware, emailGroups, sharedMailboxes, companies, departments, hodUsers));
        }

        private static EditOnboardingPageViewModel MapToViewModel(
            OnboardingRequest model,
            List<IT_Form.Models.StandardSoftware> standardSoftware,
            List<IT_Form.Models.EmailGroup> emailGroups,
            List<IT_Form.Models.SharedMailbox> sharedMailboxes,
            List<string> companies,
            List<IT_Form.Models.Department> departments,
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> hodUsers)
        {
            return new EditOnboardingPageViewModel
            {
                Id = model.Id,
                RequestDate = model.RequestDate,
                Status = model.Status,
                CreatedByUserId = model.CreatedByUserId,
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
                IssuedSoftware = model.IssuedSoftware,
                RequestSoftware = model.RequestSoftware,
                RequestEmailGroups = model.RequestEmailGroups,
                OtherGroup = model.OtherGroup,
                OtherGroupDetails = model.OtherGroupDetails,
                RequestSharedMailboxes = model.RequestSharedMailboxes,
                HasAdobe = model.HasAdobe,
                HasMSProject = model.HasMSProject,
                HasAutoDesk = model.HasAutoDesk,
                HasGStarCAD = model.HasGStarCAD,
                HasOS = model.HasOS,
                HasFortiClientVPN = model.HasFortiClientVPN,
                HasSQLAccounting = model.HasSQLAccounting,
                HasMSVisio = model.HasMSVisio,
                HasAutoCAD = model.HasAutoCAD,
                HasSketchUp = model.HasSketchUp,
                HasMSOffice = model.HasMSOffice,
                HasGoogleChrome = model.HasGoogleChrome,
                HasMSTeams = model.HasMSTeams,
                HasAntivirus = model.HasAntivirus,
                HasDWGTrueView = model.HasDWGTrueView,
                HasIFlexiHRMS = model.HasIFlexiHRMS,
                HasEShare = model.HasEShare,
                HasOtherSoftware = model.HasOtherSoftware,
                OtherSoftwareDetails = model.OtherSoftwareDetails,
                PrecastGroup = model.PrecastGroup,
                MailboxAccount = model.MailboxAccount,
                MailboxLogistic = model.MailboxLogistic,
                MailboxInfo = model.MailboxInfo,
                MailboxAdmin = model.MailboxAdmin,
                MailboxIntern = model.MailboxIntern,
                MailboxIT = model.MailboxIT,
                MailboxSupervisor = model.MailboxSupervisor,
                MailboxStore = model.MailboxStore,
                FolderPermissions = model.FolderPermissions,
                RejectionReason = model.RejectionReason,
                RejectedByUserId = model.RejectedByUserId,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt,
                CompletedAt = model.CompletedAt,
                SlaNotifyStatus = model.SlaNotifyStatus,
                AssetRemarks = model.AssetRemarks,
                SoftwareRemarks = model.SoftwareRemarks,
                FolderRemarks = model.FolderRemarks,
                OtherBranchDetails = model.OtherBranchDetails,
                OtherDepartmentDetails = model.OtherDepartmentDetails,
                OtherSharedMailboxDetails = model.OtherSharedMailboxDetails,
                HasOtherSharedMailbox = model.HasOtherSharedMailbox,
                OtherFolderDetails = model.OtherFolderDetails,
                HasOtherFolder = model.HasOtherFolder,
                IsPdpaConsented = model.IsPdpaConsented,
                PdpaConsentedAt = model.PdpaConsentedAt,
                StandardSoftwareOptions = standardSoftware,
                EmailGroupOptions = emailGroups,
                SharedMailboxOptions = sharedMailboxes,
                CompanyOptions = companies,
                DepartmentOptions = departments,
                HODUserOptions = hodUsers
            };
        }
    }
}
