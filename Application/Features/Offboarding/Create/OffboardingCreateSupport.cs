// Project comment: OffboardingCreateSupport.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.Create
{
    /// <summary>
    /// Defines the OffboardingCreateSupport class used by the IT Form application.
    /// </summary>
    internal static class OffboardingCreateSupport
    {
        private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];

        /// <summary>
        /// Validates data for the ValidateAssignedHodAsync operation.
        /// </summary>
        internal static async Task<string?> ValidateAssignedHodAsync(
            ApplicationDbContext context,
            OffboardingRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.AssignedHodId))
            {
                return null;
            }

            var assignedHod = await context.Users
                .Include(u => u.UserCompanies)
                .FirstOrDefaultAsync(
                    u => u.Id == request.AssignedHodId &&
                         u.IsActive &&
                         u.UserTitles.Any(t => t.Title == "HOD"),
                    cancellationToken);

            if (assignedHod == null)
            {
                return "Selected HOD is invalid.";
            }

            if (!string.IsNullOrWhiteSpace(request.Branch) &&
                !assignedHod.UserCompanies.Any(c => CompanyHelper.IsMatch(c.CompanyName, request.Branch)))
            {
                return "Selected HOD is not assigned to the employee company.";
            }

            request.HODEmail = assignedHod.Email ?? string.Empty;
            return null;
        }

        /// <summary>
        /// Validates data for the ValidateResignationLetter operation.
        /// </summary>
        internal static string? ValidateResignationLetter(IFormFile? resignationLetter)
        {
            if (resignationLetter == null || resignationLetter.Length <= 0)
            {
                return null;
            }

            if (resignationLetter.Length > 10 * 1024 * 1024)
            {
                return "File size exceeds the 10MB limit.";
            }

            var extension = Path.GetExtension(resignationLetter.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                return "Invalid file type. Only PDF and images (.jpg, .jpeg, .png) are allowed.";
            }

            if (!UploadFileSignatureValidator.HasAllowedSignature(resignationLetter, AllowedExtensions))
            {
                return "Invalid file content. The file signature must match an allowed PDF or image format.";
            }

            return null;
        }

        /// <summary>
        /// Saves data for the SaveResignationLetterAsync operation.
        /// </summary>
        internal static async Task SaveResignationLetterAsync(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            OffboardingRequest request,
            IFormFile? resignationLetter,
            CancellationToken cancellationToken)
        {
            if (resignationLetter == null || resignationLetter.Length <= 0)
            {
                return;
            }

            var resignationExtension = Path.GetExtension(resignationLetter.FileName).ToLowerInvariant();
            var uniqueFileName = $"Offboarding_Resignation_{request.Id}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{resignationExtension}";
            request.ResignationLetterPath = await SensitiveFileStorage.SaveAsync(
                resignationLetter,
                env,
                "resignations",
                uniqueFileName,
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Runs the FailureAsync operation.
        /// </summary>
        internal static async Task<ProcessResult<OffboardingCreatePageViewModel>> FailureAsync(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            string message,
            OffboardingRequest request,
            CancellationToken cancellationToken)
        {
            var pageModel = await BuildPageModelAsync(context, currentUserAccessor, request, cancellationToken);
            return ProcessResult<OffboardingCreatePageViewModel>.Failure(message) with { Data = pageModel };
        }

        /// <summary>
        /// Builds the value returned by the BuildPageModelAsync operation.
        /// </summary>
        private static async Task<OffboardingCreatePageViewModel> BuildPageModelAsync(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor,
            OffboardingRequest request,
            CancellationToken cancellationToken)
        {
            var pageModel = new OffboardingCreatePageViewModel
            {
                Id = request.Id,
                OnboardingRequestId = request.OnboardingRequestId,
                FullName = request.FullName,
                EmployeeID = request.EmployeeID,
                Department = request.Department,
                Branch = request.Branch,
                HODEmail = request.HODEmail,
                EmailAddress = request.EmailAddress,
                LastDayOfWork = request.LastDayOfWork,
                OtherDepartmentDetails = request.OtherDepartmentDetails,
                OtherBranchDetails = request.OtherBranchDetails,
                AssignedHodId = request.AssignedHodId
            };

            var employeesWithActiveRequests = await context.OffboardingRequests
                .Where(r => r.Status != RequestStatus.Rejected)
                .Select(r => r.OnboardingRequestId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var user = currentUserAccessor.User;
            var employeeQuery = context.OnboardingRequests
                .Where(o => (o.Status == RequestStatus.Completed || o.Status == RequestStatus.Approved) &&
                            !employeesWithActiveRequests.Contains(o.Id));

            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(user);
            if (scopedCompanies.Count > 0)
            {
                employeeQuery = CompanyScopeHelper.ApplyOnboardingCompanyScope(employeeQuery, scopedCompanies);
            }

            pageModel.Employees = await employeeQuery
                .OrderBy(o => o.FullName)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = $"{o.FullName} ({o.Department})",
                    Selected = o.Id == request.OnboardingRequestId
                })
                .ToListAsync(cancellationToken);

            pageModel.AssignedHodList = await BuildHodListAsync(context, request.Branch, request.AssignedHodId, cancellationToken);
            return pageModel;
        }

        /// <summary>
        /// Builds the value returned by the BuildHodListAsync operation.
        /// </summary>
        private static async Task<List<SelectListItem>> BuildHodListAsync(
            ApplicationDbContext context,
            string? company,
            string? selectedId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(company))
            {
                return [];
            }

            return await context.Users
                .Where(u => u.IsActive && u.UserTitles.Any(t => t.Title == "HOD"))
                .Where(u => u.UserCompanies.Any(c =>
                    c.CompanyName == company ||
                    c.CompanyName == CompanyHelper.Normalize(company) ||
                    (!company.Contains(" (") && c.CompanyName.StartsWith(company + " ("))))
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.FullName,
                    Selected = u.Id == selectedId
                })
                .ToListAsync(cancellationToken);
        }
    }
}
