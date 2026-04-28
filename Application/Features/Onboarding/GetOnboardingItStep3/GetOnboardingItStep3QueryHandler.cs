// Project comment: GetOnboardingItStep3QueryHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Application.DTOs.Options;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Features.StandardSoftware;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep3
{
    /// <summary>
    /// Handles the GetOnboardingItStep3Query query workflow.
    /// </summary>
    public sealed class GetOnboardingItStep3QueryHandler : IRequestHandler<GetOnboardingItStep3Query, GetOnboardingItStep3Result>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetOnboardingItStep3QueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<GetOnboardingItStep3Result> Handle(GetOnboardingItStep3Query request, CancellationToken cancellationToken)
        {
            var model = request.SessionModel;
            if (model == null || model.Id == 0)
            {
                return Empty(GetOnboardingItStep3Status.SessionExpired, "Session expired. Please use 'Continue IT Setup' from the request details page.");
            }

            var dbRequest = await _context.OnboardingRequests
                .AsNoTracking()
                .Where(r => r.Id == model.Id)
                .Select(r => new { r.Id, r.Status, r.Branch })
                .FirstOrDefaultAsync(cancellationToken);
            if (dbRequest == null)
            {
                return Empty(GetOnboardingItStep3Status.NotFound);
            }

            if (!CompanyScopeHelper.CanAccessCompany(_currentUserAccessor.User, dbRequest.Branch))
            {
                return Empty(GetOnboardingItStep3Status.Forbidden, "You can only access IT setup for requests in your assigned company scope.", model.Id);
            }

            if (dbRequest.Status != RequestStatus.PendingITApproval)
            {
                return Empty(GetOnboardingItStep3Status.Locked, "IT Setup can only be performed on requests in 'Pending IT Approval' status.", model.Id);
            }

            await EnsureIssuedAssetsLoadedAsync(model, cancellationToken);
            model.CurrentStep = 3;

            var companyIds = await ResolveCompanyIdsAsync(dbRequest.Branch, cancellationToken);

            var availableSoftwareQuery = _context.SoftwareLicenses
                .AsNoTracking()
                .AsQueryable();

            if (companyIds.Count > 0)
            {
                availableSoftwareQuery = availableSoftwareQuery.Where(license =>
                    !license.SoftwareLicenseCompanies.Any() ||
                    license.SoftwareLicenseCompanies.Any(link => companyIds.Contains(link.CompanyId)));
            }
            else
            {
                availableSoftwareQuery = availableSoftwareQuery.Where(license => !license.SoftwareLicenseCompanies.Any());
            }

            var availableSoftware = await availableSoftwareQuery
                .AsNoTracking()
                .OrderBy(s => s.StandardSoftware != null ? s.StandardSoftware.SoftwareName : s.SoftwareName)
                .ThenBy(s => s.SoftwareName)
                .ProjectToType<SoftwareLicenseOptionDto>()
                .ToListAsync(cancellationToken);

            var availableLicenseIds = availableSoftware
                .Select(license => license.Id)
                .ToArray();

            var standardSoftware = await _context.StandardSoftware
                .AsNoTracking()
                .Where(s => s.IsActive && s.Licenses.Any(license => availableLicenseIds.Contains(license.Id)))
                .OrderBy(s => s.SoftwareName)
                .ProjectToType<StandardSoftwareOptionDto>()
                .ToListAsync(cancellationToken);

            foreach (var software in standardSoftware)
            {
                software.Licenses = software.Licenses
                    .Where(license => availableLicenseIds.Contains(license.Id))
                    .OrderBy(license => license.SoftwareName)
                    .ToList();
            }

            var licensesWithKeys = (await _context.SoftwareKeys
                .AsNoTracking()
                .Select(key => key.SoftwareLicenseId)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var availableKeyCounts = await _context.SoftwareKeys
                .AsNoTracking()
                .Where(key => !key.IsAssigned)
                .GroupBy(key => key.SoftwareLicenseId)
                .Select(group => new { SoftwareLicenseId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.SoftwareLicenseId, x => x.Count, cancellationToken);

            var issuedSoftwareCounts = await _context.IssuedSoftware
                .AsNoTracking()
                .GroupBy(i => i.SoftwareLicenseId)
                .Select(g => new { SoftwareLicenseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SoftwareLicenseId, x => x.Count, cancellationToken);

            var licenseCounts = new Dictionary<int, int>();
            foreach (var license in availableSoftware)
            {
                var availableSeatRows = availableKeyCounts.GetValueOrDefault(license.Id, 0);
                var issuedAssignments = issuedSoftwareCounts.GetValueOrDefault(license.Id, 0);
                licenseCounts[license.Id] = SoftwareLicenseSeatSyncSupport.GetAvailableAssignmentCount(
                    license.IsFree,
                    licensesWithKeys.Contains(license.Id),
                    availableSeatRows,
                    issuedAssignments,
                    license.ContractLimit);
            }

            var requestedSoftwareIds = await _context.RequestSoftware
                .AsNoTracking()
                .Where(rs => rs.OnboardingRequestId == model.Id)
                .Select(rs => rs.StandardSoftwareId)
                .ToListAsync(cancellationToken);

            var emailGroups = await _context.EmailGroups
                .AsNoTracking()
                .Include(group => group.EmailGroupCompanies)
                .Where(group => group.IsActive &&
                                (companyIds.Count > 0
                                    ? !group.EmailGroupCompanies.Any() || group.EmailGroupCompanies.Any(link => companyIds.Contains(link.CompanyId))
                                    : !group.EmailGroupCompanies.Any()))
                .OrderBy(e => e.GroupName)
                .ProjectToType<EmailGroupOptionDto>()
                .ToListAsync(cancellationToken);
            var sharedMailboxes = await _context.SharedMailboxes
                .AsNoTracking()
                .Include(mailbox => mailbox.SharedMailboxCompanies)
                .Where(mailbox => mailbox.IsActive &&
                                  (companyIds.Count > 0
                                      ? !mailbox.SharedMailboxCompanies.Any() || mailbox.SharedMailboxCompanies.Any(link => companyIds.Contains(link.CompanyId))
                                      : !mailbox.SharedMailboxCompanies.Any()))
                .OrderBy(m => m.MailboxName)
                .ProjectToType<SharedMailboxOptionDto>()
                .ToListAsync(cancellationToken);
            var availableAssetsQuery = _context.ITAssets
                .AsNoTracking()
                .Where(a => a.Status == AssetStatus.Available)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(dbRequest.Branch))
            {
                availableAssetsQuery = CompanyScopeHelper.ApplyCompanyScope(
                    availableAssetsQuery,
                    [dbRequest.Branch],
                    asset => asset.Company);
            }

            var availableAssets = await availableAssetsQuery
                .OrderBy(a => a.AssetTag)
                .ProjectToType<ITAssetOptionDto>()
                .ToListAsync(cancellationToken);

            return new GetOnboardingItStep3Result(
                GetOnboardingItStep3Status.Ready,
                model,
                MapToViewModel(
                    model,
                    model.Id,
                    availableSoftware,
                    standardSoftware,
                    licenseCounts,
                    requestedSoftwareIds,
                    emailGroups,
                    sharedMailboxes,
                    availableAssets),
                null,
                model.Id);
        }

        /// <summary>
        /// Runs the Empty operation.
        /// </summary>
        private static GetOnboardingItStep3Result Empty(GetOnboardingItStep3Status status, string? message = null, int requestId = 0)
            => new(
                status,
                null,
                null,
                message,
                requestId);

        /// <summary>
        /// Maps data for the MapToViewModel operation.
        /// </summary>
        private static OnboardingStep3PageViewModel MapToViewModel(
            OnboardingViewModel model,
            int requestId,
            List<SoftwareLicenseOptionDto> availableSoftware,
            List<StandardSoftwareOptionDto> standardSoftware,
            Dictionary<int, int> licenseAvailability,
            List<int> requestedSoftwareIds,
            List<EmailGroupOptionDto> emailGroups,
            List<SharedMailboxOptionDto> sharedMailboxes,
            List<ITAssetOptionDto> availableAssets)
        {
            var viewModel = model.Adapt<OnboardingStep3PageViewModel>();
            viewModel.RequestId = requestId;
            viewModel.AvailableSoftware = availableSoftware;
            viewModel.StandardSoftwareOptions = standardSoftware;
            viewModel.LicenseAvailability = licenseAvailability;
            viewModel.RequestedSoftwareIds = requestedSoftwareIds;
            viewModel.EmailGroups = emailGroups;
            viewModel.SharedMailboxes = sharedMailboxes;
            viewModel.AvailableAssets = availableAssets;
            return viewModel;
        }

        /// <summary>
        /// Runs the EnsureIssuedAssetsLoadedAsync operation.
        /// </summary>
        private async Task EnsureIssuedAssetsLoadedAsync(OnboardingViewModel model, CancellationToken cancellationToken)
        {
            if (model.IssuedAssets != null && model.IssuedAssets.Any())
            {
                return;
            }

            model.IssuedAssets = await _context.IssuedAssets
                .AsNoTracking()
                .Where(asset => asset.OnboardingRequestId == model.Id)
                .OrderBy(asset => asset.Id)
                .Select(asset => new AssetItemViewModel
                {
                    AssetType = asset.AssetType,
                    OtherAssetType = asset.OtherAssetType,
                    ITAssetId = asset.ITAssetId,
                    Description = asset.Description,
                    SerialNumber = asset.SerialNumber,
                    AssetTag = asset.AssetTag,
                    Qty = asset.Qty,
                    BrandModel = asset.BrandModel,
                    IsChecked = true
                })
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Runs the ResolveCompanyIdsAsync operation.
        /// </summary>
        private async Task<List<int>> ResolveCompanyIdsAsync(string? companyName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(companyName))
            {
                return [];
            }

            var companies = await _context.Companies
                .AsNoTracking()
                .Select(company => new
                {
                    company.Id,
                    DisplayName = string.IsNullOrWhiteSpace(company.Lot)
                        ? company.Name
                        : company.Name + " (" + company.Lot + ")"
                })
                .ToListAsync(cancellationToken);

            return companies
                .Where(company => CompanyHelper.IsMatch(company.DisplayName, companyName))
                .Select(company => company.Id)
                .Distinct()
                .ToList();
        }
    }
}
