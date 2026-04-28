// Project comment: UpdateOnboardingItStep3CommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Helpers;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep3
{
    /// <summary>
    /// Handles the UpdateOnboardingItStep3Command command workflow.
    /// </summary>
    public sealed class UpdateOnboardingItStep3CommandHandler
        : IRequestHandler<UpdateOnboardingItStep3Command, UpdateOnboardingItStep3Result>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public UpdateOnboardingItStep3CommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<UpdateOnboardingItStep3Result> Handle(UpdateOnboardingItStep3Command request, CancellationToken cancellationToken)
        {
            var session = request.Session;

            var onboardingRequest = await _context.OnboardingRequests
                .Include(r => r.IssuedAssets)
                .Include(r => r.FolderPermissions)
                .Include(r => r.RequestSoftware)
                .Include(r => r.RequestEmailGroups)
                .Include(r => r.RequestSharedMailboxes)
                .FirstOrDefaultAsync(r => r.Id == session.Id, cancellationToken);

            if (onboardingRequest == null)
            {
                return new UpdateOnboardingItStep3Result(
                    UpdateOnboardingItStep3Status.NotFound,
                    session.Id,
                    "Request not found.");
            }

            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, onboardingRequest.Branch))
            {
                return new UpdateOnboardingItStep3Result(
                    UpdateOnboardingItStep3Status.Forbidden,
                    onboardingRequest.Id,
                    "You can only access IT setup for requests in your assigned company scope.");
            }

            if (onboardingRequest.Status != RequestStatus.PendingITApproval)
            {
                return new UpdateOnboardingItStep3Result(
                    UpdateOnboardingItStep3Status.Locked,
                    onboardingRequest.Id,
                    "Cannot update IT Setup - request is not in 'Pending IT Approval' status.");
            }

            session.SelectedStandardSoftwareIds = request.Model.SelectedStandardSoftwareIds ?? new List<int>();
            session.SelectedSoftwareIds = request.Model.SelectedSoftwareIds ?? new List<int>();

            var selectedSoftwareNames = await ResolveSoftwareSelectionNamesAsync(
                session.SelectedStandardSoftwareIds,
                session.SelectedSoftwareIds,
                cancellationToken);

            ApplyLegacySoftwareFlags(
                session,
                selectedSoftwareNames.StandardSoftwareNames,
                selectedSoftwareNames.LicenseSoftwareNames,
                request.Model.HasOtherSoftware,
                request.Model.OtherSoftwareDetails);

            session.SelectedEmailGroupIds = request.Model.SelectedEmailGroupIds ?? new List<int>();
            session.OtherGroup = request.Model.OtherGroup;
            session.OtherGroupDetails = request.Model.OtherGroupDetails;
            session.SelectedSharedMailboxIds = request.Model.SelectedSharedMailboxIds ?? new List<int>();
            session.OtherSharedMailboxDetails = request.Model.OtherSharedMailboxDetails;
            session.SoftwareRemarks = request.Model.SoftwareRemarks;
            session.MailboxAccount = request.Model.MailboxAccount;
            session.MailboxLogistic = request.Model.MailboxLogistic;
            session.MailboxInfo = request.Model.MailboxInfo;
            session.MailboxAdmin = request.Model.MailboxAdmin;
            session.MailboxIntern = request.Model.MailboxIntern;
            session.MailboxIT = request.Model.MailboxIT;
            session.MailboxSupervisor = request.Model.MailboxSupervisor;
            session.MailboxStore = request.Model.MailboxStore;
            session.PrecastGroup = request.Model.PrecastGroup;

            if (request.Model.IssuedAssets != null && session.IssuedAssets != null)
            {
                for (var i = 0; i < Math.Min(request.Model.IssuedAssets.Count, session.IssuedAssets.Count); i++)
                {
                    session.IssuedAssets[i].OtherAssetType = request.Model.IssuedAssets[i].OtherAssetType ?? session.IssuedAssets[i].OtherAssetType;
                    session.IssuedAssets[i].ITAssetId = request.Model.IssuedAssets[i].ITAssetId;
                    session.IssuedAssets[i].Qty = request.Model.IssuedAssets[i].Qty;
                    session.IssuedAssets[i].Description = request.Model.IssuedAssets[i].Description;
                }
            }

            session.CurrentStep = 4;

            if (request.IsDraft)
            {
                await OnboardingItSetupPersistence.SaveDraftAsync(_context, onboardingRequest, session, cancellationToken);
                return new UpdateOnboardingItStep3Result(
                    UpdateOnboardingItStep3Status.DraftSaved,
                    onboardingRequest.Id,
                    "Draft saved successfully.");
            }

            return new UpdateOnboardingItStep3Result(
                UpdateOnboardingItStep3Status.ReadyForStep4,
                onboardingRequest.Id,
                "Step 3 saved.");
        }

        private async Task<(HashSet<string> StandardSoftwareNames, HashSet<string> LicenseSoftwareNames)> ResolveSoftwareSelectionNamesAsync(
            IEnumerable<int>? selectedStandardSoftwareIds,
            IEnumerable<int>? selectedSoftwareLicenseIds,
            CancellationToken cancellationToken)
        {
            var standardIds = (selectedStandardSoftwareIds ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            var licenseIds = (selectedSoftwareLicenseIds ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var standardSoftwareNames = standardIds.Any()
                ? await _context.StandardSoftware
                    .Where(s => standardIds.Contains(s.Id))
                    .Select(s => s.SoftwareName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n!.Trim())
                    .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var licenseSoftwareNames = licenseIds.Any()
                ? await _context.SoftwareLicenses
                    .Where(l => licenseIds.Contains(l.Id))
                    .Select(l => l.SoftwareName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n!.Trim())
                    .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return (standardSoftwareNames, licenseSoftwareNames);
        }

        /// <summary>
        /// Runs the ContainsSoftware operation.
        /// </summary>
        private static bool ContainsSoftware(IEnumerable<string> softwareNames, params string[] keywords)
        {
            foreach (var name in softwareNames)
            {
                if (keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Applies changes for the ApplyLegacySoftwareFlags operation.
        /// </summary>
        private static void ApplyLegacySoftwareFlags(
            OnboardingViewModel target,
            IReadOnlyCollection<string> standardSoftwareNames,
            IReadOnlyCollection<string> licenseSoftwareNames,
            bool hasOtherSoftware,
            string? otherSoftwareDetails)
        {
            var allSoftwareNames = standardSoftwareNames
                .Concat(licenseSoftwareNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            target.HasAdobe = ContainsSoftware(allSoftwareNames, "adobe");
            target.HasMSProject = ContainsSoftware(allSoftwareNames, "project");
            target.HasAutoDesk = ContainsSoftware(allSoftwareNames, "autodesk");
            target.HasGStarCAD = ContainsSoftware(allSoftwareNames, "gstarcad");
            target.HasOS = ContainsSoftware(allSoftwareNames, "windows", "operating system");
            target.HasFortiClientVPN = ContainsSoftware(allSoftwareNames, "forticlient", "vpn");
            target.HasSQLAccounting = ContainsSoftware(allSoftwareNames, "sql accounting");
            target.HasMSVisio = ContainsSoftware(allSoftwareNames, "visio");
            target.HasAutoCAD = ContainsSoftware(allSoftwareNames, "autocad");
            target.HasSketchUp = ContainsSoftware(allSoftwareNames, "sketchup");
            target.HasMSOffice = ContainsSoftware(allSoftwareNames, "office", "microsoft 365", "ms365");
            target.HasGoogleChrome = ContainsSoftware(allSoftwareNames, "chrome");
            target.HasMSTeams = ContainsSoftware(allSoftwareNames, "teams");
            target.HasAntivirus = ContainsSoftware(allSoftwareNames, "antivirus", "defender", "endpoint");
            target.HasDWGTrueView = ContainsSoftware(allSoftwareNames, "dwg trueview", "trueview");
            target.HasIFlexiHRMS = ContainsSoftware(allSoftwareNames, "iflexi", "hrms");
            target.HasEShare = ContainsSoftware(allSoftwareNames, "eshare");
            target.HasOtherSoftware = hasOtherSoftware;
            target.OtherSoftwareDetails = otherSoftwareDetails;
        }
    }
}
