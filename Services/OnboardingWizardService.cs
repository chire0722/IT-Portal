using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace IT_Form.Services
{
    public sealed class OnboardingWizardService : IOnboardingWizardService
    {
        private const string SessionKey = "OnboardingWizard";
        private readonly ApplicationDbContext _context;

        public OnboardingWizardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public OnboardingViewModel GetFromSession(ISession session)
        {
            var json = session.GetString(SessionKey);
            var model = string.IsNullOrEmpty(json)
                ? new OnboardingViewModel()
                : JsonConvert.DeserializeObject<OnboardingViewModel>(json) ?? new OnboardingViewModel();

            if (model.FolderPermissions.Count == 0)
            {
                var activeFolders = _context.ServerFolders.Where(s => s.IsActive).Select(f => f.FolderName).ToList();
                model.InitializeDefaultFolderPermissions(activeFolders);
            }

            return model;
        }

        public void SaveToSession(ISession session, OnboardingViewModel model)
        {
            var json = JsonConvert.SerializeObject(model);
            session.SetString(SessionKey, json);
        }

        public async Task<OnboardingViewModel> CreateNewSessionAsync(CancellationToken cancellationToken = default)
        {
            var model = new OnboardingViewModel();
            model.InitializeDefaultFolderPermissions(await GetActiveFolderNamesAsync(cancellationToken));
            return model;
        }

        public async Task<OnboardingViewModel?> RestoreDraftSessionAsync(int requestId, CancellationToken cancellationToken = default)
        {
            var request = await _context.OnboardingRequests
                .Include(r => r.IssuedAssets)
                .Include(r => r.FolderPermissions)
                .Include(r => r.RequestSoftware)
                .Include(r => r.RequestEmailGroups)
                .Include(r => r.RequestSharedMailboxes)
                .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

            if (request == null)
            {
                return null;
            }

            var session = new OnboardingViewModel();
            MapRequestToSession(request, session);

            if (session.FolderPermissions.Count == 0)
            {
                session.InitializeDefaultFolderPermissions(await GetActiveFolderNamesAsync(cancellationToken));
            }

            return session;
        }

        public async Task<OnboardingViewModel?> RestoreItSetupSessionAsync(int requestId, CancellationToken cancellationToken = default)
        {
            var request = await _context.OnboardingRequests
                .Include(r => r.IssuedAssets)
                .Include(r => r.FolderPermissions)
                .Include(r => r.RequestSoftware)
                .Include(r => r.RequestEmailGroups)
                    .ThenInclude(eg => eg.EmailGroup)
                .Include(r => r.RequestSharedMailboxes)
                .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

            if (request == null)
            {
                return null;
            }

            var session = new OnboardingViewModel();
            MapRequestToSession(request, session);
            session.SelectedSoftwareIds = await _context.IssuedSoftware
                .Where(i => i.OnboardingRequestId == request.Id)
                .Select(i => i.SoftwareLicenseId)
                .ToListAsync(cancellationToken);

            session.IssuedAssets = request.IssuedAssets?.Select(a => new AssetItemViewModel
            {
                AssetType = a.AssetType,
                ITAssetId = a.ITAssetId,
                Description = a.Description,
                IsChecked = true,
                Qty = a.Qty
            }).ToList() ?? new List<AssetItemViewModel>();

            if (session.FolderPermissions.Count == 0)
            {
                session.InitializeDefaultFolderPermissions(await GetActiveFolderNamesAsync(cancellationToken));
            }

            session.CurrentStep = 3;
            return session;
        }

        public async Task<OnboardingViewModel> PrepareDraftSessionAsync(OnboardingViewModel currentSession, OnboardingViewModel postedModel, CancellationToken cancellationToken = default)
        {
            var session = currentSession ?? new OnboardingViewModel();

            if (session.Id == 0 && postedModel.Id > 0)
            {
                var existingSession = await RestoreDraftSessionAsync(postedModel.Id, cancellationToken);
                if (existingSession != null)
                {
                    session = existingSession;
                }
            }

            MapStep1ToSession(postedModel, session);
            session.CurrentStep = 1;
            ClearHrRestrictedAccessFields(session);
            return session;
        }

        public OnboardingViewModel PrepareStep2Session(OnboardingViewModel model)
        {
            model.CurrentStep = 2;

            if (model.IssuedAssets == null || model.IssuedAssets.Count == 0)
            {
                model.IssuedAssets = new List<AssetItemViewModel>
                {
                    new() { AssetType = AssetType.Laptop },
                    new() { AssetType = AssetType.DesktopPC },
                    new() { AssetType = AssetType.Monitor },
                    new() { AssetType = AssetType.USBLANAdaptor }
                };
            }

            return model;
        }

        public async Task<OnboardingViewModel> EnsureStep4FolderPermissionsAsync(OnboardingViewModel model, string? branchName, CancellationToken cancellationToken = default)
        {
            model.CurrentStep = 4;

            if (model.FolderPermissions == null || model.FolderPermissions.Count == 0)
            {
                var activeFolders = await _context.ServerFolders
                    .Include(s => s.ServerFolderCompanies)
                        .ThenInclude(fc => fc.Company)
                    .Where(s => s.IsActive && (!s.ServerFolderCompanies.Any() ||
                                              s.ServerFolderCompanies.Any(fc => branchName != null &&
                                                                               (fc.Company.Name == branchName ||
                                                                                branchName.StartsWith(fc.Company.Name + " (")))))
                    .Select(f => f.FolderName)
                    .ToListAsync(cancellationToken);

                model.InitializeDefaultFolderPermissions(activeFolders);
            }

            return model;
        }

        public void MapRequestToSession(OnboardingRequest request, OnboardingViewModel session)
        {
            session.Id = request.Id;
            session.FullName = request.FullName ?? string.Empty;
            session.EmployeeID = request.EmployeeID ?? string.Empty;
            session.Designation = request.Designation ?? string.Empty;
            session.Department = request.Department ?? string.Empty;
            session.Branch = request.Branch ?? string.Empty;
            session.DeskNumber = request.DeskNumber ?? string.Empty;
            session.JoinDate = request.JoinDate;
            session.EmailAddress = request.EmailAddress ?? string.Empty;
            session.ContactNumber = request.ContactNumber ?? string.Empty;
            session.HODEmail = request.HODEmail ?? string.Empty;
            session.IsPdpaConsented = request.IsPdpaConsented;

            session.SelectedStandardSoftwareIds = request.RequestSoftware?.Select(r => r.StandardSoftwareId).ToList() ?? new List<int>();
            session.SelectedEmailGroupIds = request.RequestEmailGroups?.Select(r => r.EmailGroupId).ToList() ?? new List<int>();
            session.SelectedSharedMailboxIds = request.RequestSharedMailboxes?.Select(r => r.SharedMailboxId).ToList() ?? new List<int>();

            session.OtherGroup = request.OtherGroup;
            session.OtherGroupDetails = request.OtherGroupDetails;
            session.OtherSharedMailboxDetails = request.OtherSharedMailboxDetails;
            session.OtherFolderDetails = request.OtherFolderDetails;
            session.OtherBranchDetails = request.OtherBranchDetails;
            session.OtherDepartmentDetails = request.OtherDepartmentDetails;
            session.AssetRemarks = request.AssetRemarks;
            session.SoftwareRemarks = request.SoftwareRemarks;
            session.FolderRemarks = request.FolderRemarks;

            session.IssuedAssets = request.IssuedAssets?
                .Select(a => new AssetItemViewModel
                {
                    AssetType = a.AssetType,
                    Description = a.Description,
                    SerialNumber = a.SerialNumber,
                    AssetTag = a.AssetTag,
                    Qty = a.Qty,
                    BrandModel = a.BrandModel,
                    IsChecked = true
                })
                .ToList() ?? new List<AssetItemViewModel>();

            session.FolderPermissions = request.FolderPermissions?
                .Select(f => new FolderPermissionViewModel
                {
                    FolderName = f.FolderName,
                    CanRead = f.CanRead,
                    CanModify = f.CanModify
                }).ToList() ?? new List<FolderPermissionViewModel>();

            session.HasAdobe = request.HasAdobe;
            session.HasMSProject = request.HasMSProject;
            session.HasAutoDesk = request.HasAutoDesk;
            session.HasGStarCAD = request.HasGStarCAD;
            session.HasFortiClientVPN = request.HasFortiClientVPN;
            session.HasSQLAccounting = request.HasSQLAccounting;
            session.HasMSVisio = request.HasMSVisio;
            session.HasAutoCAD = request.HasAutoCAD;
            session.HasSketchUp = request.HasSketchUp;
            session.HasMSOffice = request.HasMSOffice;
            session.HasGoogleChrome = request.HasGoogleChrome;
            session.HasMSTeams = request.HasMSTeams;
            session.HasAntivirus = request.HasAntivirus;
            session.HasDWGTrueView = request.HasDWGTrueView;
            session.HasIFlexiHRMS = request.HasIFlexiHRMS;
            session.HasEShare = request.HasEShare;
            session.HasOtherSoftware = request.HasOtherSoftware;
            session.OtherSoftwareDetails = request.OtherSoftwareDetails;

            session.MailboxAccount = request.MailboxAccount;
            session.MailboxLogistic = request.MailboxLogistic;
            session.MailboxInfo = request.MailboxInfo;
            session.MailboxAdmin = request.MailboxAdmin;
            session.MailboxIntern = request.MailboxIntern;
            session.MailboxIT = request.MailboxIT;
            session.MailboxSupervisor = request.MailboxSupervisor;
            session.MailboxStore = request.MailboxStore;
            session.PrecastGroup = request.PrecastGroup;
        }

        public void ClearHrRestrictedAccessFields(OnboardingViewModel session)
        {
            session.FolderPermissions = new List<FolderPermissionViewModel>();
            session.FolderRemarks = null;
            session.HasOtherFolder = false;
            session.OtherFolderDetails = null;
        }

        public void MapStep1ToSession(OnboardingViewModel model, OnboardingViewModel session)
        {
            if (model.Id > 0)
            {
                session.Id = model.Id;
            }

            session.FullName = model.FullName;
            session.EmployeeID = model.EmployeeID;
            session.Designation = model.Designation;
            session.Department = model.Department;
            session.Branch = model.Branch;
            session.DeskNumber = model.DeskNumber;
            session.JoinDate = model.JoinDate;
            session.EmailAddress = model.EmailAddress;
            session.ContactNumber = model.ContactNumber;
            session.HODEmail = model.HODEmail;
            session.IsPdpaConsented = model.IsPdpaConsented;
            session.OtherDepartmentDetails = model.OtherDepartmentDetails;
            session.OtherBranchDetails = model.OtherBranchDetails;
            // The view model only tracks the consent flag; timestamps live on the request entity.
        }

        public void MapSessionToRequest(OnboardingViewModel session, OnboardingRequest request)
        {
            request.FullName = session.FullName;
            request.EmployeeID = session.EmployeeID;
            request.Designation = session.Designation;
            request.Department = session.Department;
            request.Branch = session.Branch;
            request.DeskNumber = session.DeskNumber;
            request.JoinDate = session.JoinDate;
            request.EmailAddress = session.EmailAddress;
            request.ContactNumber = session.ContactNumber;
            request.HODEmail = session.HODEmail;
            request.IsPdpaConsented = session.IsPdpaConsented;
            if (request.IsPdpaConsented && !request.PdpaConsentedAt.HasValue)
            {
                request.PdpaConsentedAt = DateTime.UtcNow;
            }

            if (request.Id > 0 && request.RequestSoftware != null && request.RequestSoftware.Any())
            {
                _context.RequestSoftware.RemoveRange(request.RequestSoftware);
            }
            request.RequestSoftware = (session.SelectedStandardSoftwareIds ?? new List<int>())
                .Distinct()
                .Select(id => new { Id = id, Software = _context.StandardSoftware.Find(id) })
                .Where(x => x.Software != null)
                .Select(x => new RequestSoftware
                {
                    StandardSoftwareId = x.Id,
                    SoftwareName = x.Software!.SoftwareName
                })
                .ToList();

            if (request.Id > 0 && request.RequestEmailGroups != null && request.RequestEmailGroups.Any())
            {
                _context.RequestEmailGroups.RemoveRange(request.RequestEmailGroups);
            }
            request.RequestEmailGroups = (session.SelectedEmailGroupIds ?? new List<int>())
                .Distinct()
                .Select(id => new { Id = id, Group = _context.EmailGroups.Find(id) })
                .Where(x => x.Group != null)
                .Select(x => new RequestEmailGroup
                {
                    EmailGroupId = x.Id,
                    GroupName = x.Group!.GroupName,
                    GroupEmail = x.Group.GroupEmail
                })
                .ToList();

            request.OtherGroup = session.OtherGroup;
            request.OtherGroupDetails = session.OtherGroupDetails;

            if (request.Id > 0 && request.RequestSharedMailboxes != null && request.RequestSharedMailboxes.Any())
            {
                _context.RequestSharedMailboxes.RemoveRange(request.RequestSharedMailboxes);
            }
            request.RequestSharedMailboxes = (session.SelectedSharedMailboxIds ?? new List<int>())
                .Distinct()
                .Select(id => new { Id = id, Mailbox = _context.SharedMailboxes.Find(id) })
                .Where(x => x.Mailbox != null)
                .Select(x => new RequestSharedMailbox
                {
                    SharedMailboxId = x.Id,
                    MailboxName = x.Mailbox!.MailboxName,
                    MailboxAddress = x.Mailbox.MailboxAddress
                })
                .ToList();

            if (request.Id > 0 && request.IssuedAssets != null && request.IssuedAssets.Any())
            {
                _context.IssuedAssets.RemoveRange(request.IssuedAssets);
            }
            request.IssuedAssets = session.IssuedAssets?
                .Where(a => a.IsChecked && (a.AssetType != AssetType.Others || !string.IsNullOrWhiteSpace(a.Description)))
                .Select(a => new IssuedAsset
                {
                    AssetType = a.AssetType,
                    Description = a.Description,
                    SerialNumber = a.SerialNumber,
                    AssetTag = a.AssetTag,
                    Qty = a.Qty,
                    BrandModel = a.BrandModel
                }).ToList() ?? new List<IssuedAsset>();

            if (request.Id > 0 && request.FolderPermissions != null && request.FolderPermissions.Any())
            {
                _context.FolderPermissions.RemoveRange(request.FolderPermissions);
            }
            request.FolderPermissions = session.FolderPermissions?.Select(f => new FolderPermission
            {
                FolderName = f.FolderName,
                CanRead = f.CanRead,
                CanModify = f.CanModify
            }).ToList() ?? new List<FolderPermission>();

            request.AssetRemarks = session.AssetRemarks;
            request.SoftwareRemarks = session.SoftwareRemarks;
            request.FolderRemarks = session.FolderRemarks;
            request.OtherBranchDetails = session.OtherBranchDetails;
            request.OtherDepartmentDetails = session.OtherDepartmentDetails;
            request.OtherSharedMailboxDetails = session.OtherSharedMailboxDetails;
            request.HasOtherSharedMailbox = !string.IsNullOrEmpty(session.OtherSharedMailboxDetails);
            request.OtherFolderDetails = session.OtherFolderDetails;
            request.HasOtherFolder = !string.IsNullOrEmpty(session.OtherFolderDetails);

            request.HasAdobe = session.HasAdobe;
            request.HasMSProject = session.HasMSProject;
            request.HasAutoDesk = session.HasAutoDesk;
            request.HasGStarCAD = session.HasGStarCAD;
            request.HasFortiClientVPN = session.HasFortiClientVPN;
            request.HasSQLAccounting = session.HasSQLAccounting;
            request.HasMSVisio = session.HasMSVisio;
            request.HasAutoCAD = session.HasAutoCAD;
            request.HasSketchUp = session.HasSketchUp;
            request.HasMSOffice = session.HasMSOffice;
            request.HasGoogleChrome = session.HasGoogleChrome;
            request.HasMSTeams = session.HasMSTeams;
            request.HasAntivirus = session.HasAntivirus;
            request.HasDWGTrueView = session.HasDWGTrueView;
            request.HasIFlexiHRMS = session.HasIFlexiHRMS;
            request.HasEShare = session.HasEShare;
            request.HasOtherSoftware = session.HasOtherSoftware;
            request.OtherSoftwareDetails = session.OtherSoftwareDetails;

            request.MailboxAccount = session.MailboxAccount;
            request.MailboxLogistic = session.MailboxLogistic;
            request.MailboxInfo = session.MailboxInfo;
            request.MailboxAdmin = session.MailboxAdmin;
            request.MailboxIntern = session.MailboxIntern;
            request.MailboxIT = session.MailboxIT;
            request.MailboxSupervisor = session.MailboxSupervisor;
            request.MailboxStore = session.MailboxStore;
            request.PrecastGroup = session.PrecastGroup;
        }

        private async Task<List<string>> GetActiveFolderNamesAsync(CancellationToken cancellationToken)
        {
            return await _context.ServerFolders
                .Where(s => s.IsActive)
                .Select(f => f.FolderName)
                .ToListAsync(cancellationToken);
        }
    }
}
