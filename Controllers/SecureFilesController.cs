// Project comment: SecureFilesController.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Controllers
{
    [Authorize]
    /// <summary>
    /// Handles web requests for the SecureFiles area.
    /// </summary>
    public class SecureFilesController : Controller
    {
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        public SecureFilesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        [HttpGet("/secure-files/{category}/{fileName}")]
        /// <summary>
        /// Runs the Download operation.
        /// </summary>
        public async Task<IActionResult> Download(string category, string fileName)
        {
            if (!SensitiveFileStorage.IsSensitiveCategory(category))
            {
                return NotFound();
            }

            var storedPath = SensitiveFileStorage.BuildStoredPath(category, fileName);

            var canAccess = category switch
            {
                SensitiveFileStorage.ResignationsCategory => await CanAccessOffboardingFileAsync(storedPath, isResignation: true),
                SensitiveFileStorage.OffboardingCategory => await CanAccessOffboardingFileAsync(storedPath, isResignation: false),
                _ => false
            };

            if (!canAccess)
            {
                return Forbid();
            }

            var physicalPath = SensitiveFileStorage.ResolvePhysicalPath(_environment, storedPath);
            if (string.IsNullOrWhiteSpace(physicalPath) || !System.IO.File.Exists(physicalPath))
            {
                return NotFound();
            }

            Response.Headers["Cache-Control"] = "private, no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";

            if (!ContentTypeProvider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                // Sensitive PDFs are downloaded as attachments instead of being rendered inline.
                return PhysicalFile(physicalPath, contentType, Path.GetFileName(fileName), enableRangeProcessing: true);
            }

            return PhysicalFile(physicalPath, contentType, enableRangeProcessing: true);
        }

        /// <summary>
        /// Runs the CanAccessOffboardingFileAsync operation.
        /// </summary>
        private async Task<bool> CanAccessOffboardingFileAsync(string storedPath, bool isResignation)
        {
            var request = await _context.OffboardingRequests
                .Include(r => r.AssignedHod)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => isResignation
                    ? r.ResignationLetterPath == storedPath
                    : r.HardwareDamageImagePath == storedPath || r.HardwareReturnImagePath == storedPath);

            if (request == null)
            {
                return false;
            }

            if (!CompanyScopeHelper.CanAccessCompany(User, request.Branch))
            {
                return false;
            }

            if (User.HasClaim("UserTitle", "IT") || User.HasClaim("UserTitle", "HR"))
            {
                return true;
            }

            var username = User.Identity?.Name ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(request.CreatedByUserId) &&
                string.Equals(request.CreatedByUserId, username, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrWhiteSpace(request.AssignedHodId))
            {
                return string.Equals(request.AssignedHodId, userId, StringComparison.Ordinal);
            }

            var hodEmailsLower = await UserHelper.GetHODEmailsAsync(_context, username);
            return request.HODEmail != null && hodEmailsLower.Contains(request.HODEmail.ToLowerInvariant());
        }
    }
}
