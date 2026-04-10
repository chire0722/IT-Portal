using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingEditOptions
{
    public sealed class GetOnboardingEditOptionsQueryHandler
        : IRequestHandler<GetOnboardingEditOptionsQuery, EditOnboardingPageViewModel>
    {
        private readonly ApplicationDbContext _context;
        private readonly IOnboardingHodService _onboardingHodService;

        public GetOnboardingEditOptionsQueryHandler(
            ApplicationDbContext context,
            IOnboardingHodService onboardingHodService)
        {
            _context = context;
            _onboardingHodService = onboardingHodService;
        }

        public async Task<EditOnboardingPageViewModel> Handle(GetOnboardingEditOptionsQuery request, CancellationToken cancellationToken)
        {
            var model = request.Model;

            var editCompanyId = !string.IsNullOrEmpty(model.Branch)
                ? await _context.Companies
                    .AsNoTracking()
                    .Where(c => c.Name == model.Branch || c.DisplayName == model.Branch)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

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
            var companies = await _context.Companies
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => c.DisplayName)
                .ToListAsync(cancellationToken);
            var departments = await _context.Departments
                .AsNoTracking()
                .Where(d => d.IsActive)
                .Include(d => d.DepartmentCompanies)
                    .ThenInclude(dc => dc.Company)
                .ToListAsync(cancellationToken);
            var hodUsers = await _onboardingHodService.BuildHodSelectListAsync(
                model.Branch,
                model.HODEmail,
                cancellationToken);

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
                HODUserOptions = hodUsers.Items.Cast<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>().ToList()
            };
        }
    }
}
