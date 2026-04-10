using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace IT_Form.Application.Features.StandardSoftware.AssignSoftwareKey
{
    public sealed class AssignSoftwareKeyCommandHandler : IRequestHandler<AssignSoftwareKeyCommand, AssignSoftwareKeyResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILicenseEncryptionService _encryptionService;
        private readonly IEmailBackgroundQueue _emailBackgroundQueue;

        public AssignSoftwareKeyCommandHandler(
            ApplicationDbContext context,
            ILicenseEncryptionService encryptionService,
            IEmailBackgroundQueue emailBackgroundQueue)
        {
            _context = context;
            _encryptionService = encryptionService;
            _emailBackgroundQueue = emailBackgroundQueue;
        }

        public async Task<AssignSoftwareKeyResult> Handle(AssignSoftwareKeyCommand request, CancellationToken cancellationToken)
        {
            var key = await _context.SoftwareKeys.Include(k => k.SoftwareLicense).FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken);
            if (key == null)
            {
                return new AssignSoftwareKeyResult(AssignSoftwareKeyStatus.NotFound, "Key not found.");
            }
            var previousOnboardingRequestId = key.OnboardingRequestId;

            if (request.OnboardingRequestId.HasValue && request.OnboardingRequestId.Value > 0)
            {
                var employee = await _context.OnboardingRequests.FindAsync(new object[] { request.OnboardingRequestId.Value }, cancellationToken);
                if (employee == null)
                {
                    return new AssignSoftwareKeyResult(AssignSoftwareKeyStatus.EmployeeNotFound, "Employee not found.");
                }

                key.OnboardingRequestId = request.OnboardingRequestId.Value;
                key.IsAssigned = true;

                if (key.SoftwareLicense!.IsSubscription)
                {
                    string emailToUse = !string.IsNullOrWhiteSpace(employee.EmailAddress) ? employee.EmailAddress! : employee.FullName ?? "Unknown";
                    string linkedString = $"[Account-Linked]: {emailToUse}";
                    key.KeyValue = _encryptionService.EncryptKey(linkedString);

                    using var sha256 = SHA256.Create();
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(linkedString));
                    key.KeyValueHash = Convert.ToBase64String(bytes);
                }

                var issuedExists = await _context.IssuedSoftware.AnyAsync(i =>
                    i.OnboardingRequestId == request.OnboardingRequestId.Value &&
                    i.SoftwareLicenseId == key.SoftwareLicenseId, cancellationToken);

                if (!issuedExists)
                {
                    _context.IssuedSoftware.Add(new IT_Form.Models.IssuedSoftware
                    {
                        OnboardingRequestId = request.OnboardingRequestId.Value,
                        SoftwareLicenseId = key.SoftwareLicenseId,
                        SoftwareName = key.SoftwareLicense?.SoftwareName ?? string.Empty
                    });
                }

                await _context.SaveChangesAsync(cancellationToken);
                if (await QueueLowLicenseAvailabilityAsync(key.SoftwareLicenseId, cancellationToken))
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                key.OnboardingRequestId = null;
                key.IsAssigned = false;

                if (key.SoftwareLicense!.IsSubscription)
                {
                    string emptySeatStr = "Seat-" + Guid.NewGuid().ToString().Substring(0, 8);
                    key.KeyValue = _encryptionService.EncryptKey(emptySeatStr);

                    using var sha256 = SHA256.Create();
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(emptySeatStr));
                    key.KeyValueHash = Convert.ToBase64String(bytes);
                }

                if (previousOnboardingRequestId.HasValue)
                {
                    var hasOtherAssignedKeys = await _context.SoftwareKeys.AnyAsync(k =>
                        k.SoftwareLicenseId == key.SoftwareLicenseId &&
                        k.OnboardingRequestId == previousOnboardingRequestId.Value &&
                        k.IsAssigned &&
                        k.Id != key.Id, cancellationToken);

                    if (!hasOtherAssignedKeys)
                    {
                        var issuedRows = await _context.IssuedSoftware
                            .Where(i => i.OnboardingRequestId == previousOnboardingRequestId.Value &&
                                        i.SoftwareLicenseId == key.SoftwareLicenseId)
                            .ToListAsync(cancellationToken);
                        if (issuedRows.Count > 0)
                        {
                            _context.IssuedSoftware.RemoveRange(issuedRows);
                        }
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            return new AssignSoftwareKeyResult(AssignSoftwareKeyStatus.AssignedOrUnassigned, "Updated successfully.");
        }

        private async Task<bool> QueueLowLicenseAvailabilityAsync(int softwareLicenseId, CancellationToken cancellationToken)
        {
            var license = await _context.SoftwareLicenses
                .Include(l => l.Keys)
                .FirstOrDefaultAsync(l => l.Id == softwareLicenseId, cancellationToken);

            if (license == null)
            {
                return false;
            }

            int assignedKeys = license.Keys.Count(k => k.IsAssigned);
            int totalSeats = (license.IsSubscription && !license.IsFree)
                ? Math.Max(license.ContractLimit, 0)
                : license.Keys.Count;
            int remainingSeats = Math.Max(totalSeats - assignedKeys, 0);
            double percentageRemaining = totalSeats > 0 ? (double)remainingSeats / totalSeats : 0;

            if (totalSeats > 0 && (remainingSeats <= 5 || percentageRemaining <= 0.10))
            {
                var itUsers = await _context.Users
                    .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email))
                    .Join(_context.UserTitles,
                        user => user.Id,
                        title => title.UserId,
                        (user, title) => new { User = user, Title = title })
                    .Where(x => x.Title.Title == "IT")
                    .Select(x => x.User.Email)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var recipients = itUsers.Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
                if (!recipients.Any())
                {
                    return false;
                }

                string toAddresses = string.Join(";", recipients);
                string subject = $"LOW LICENSE ALERT: {license.SoftwareName}";
                string message = $@"
                        <div style='font-family: Arial, sans-serif; color: #333;'>
                            <h3 style='color: #d9534f;'>Low Software License Availability</h3>
                            <p>The software license for <strong>{license.SoftwareName}</strong> is running low on available seats.</p>
                            <ul>
                                <li><strong>Total Seats:</strong> {totalSeats}</li>
                                <li><strong>Assigned Seats:</strong> {assignedKeys}</li>
                                <li><strong>Remaining Seats:</strong> <span style='color: #d9534f; font-weight: bold;'>{remainingSeats}</span></li>
                            </ul>
                            <p>Please review the license inventory and purchase additional seats if necessary.</p>
                            <hr />
                            <p><strong>Vendor:</strong> {(string.IsNullOrEmpty(license.VendorName) ? "N/A" : license.VendorName)}</p>
                            <p><strong>Support Contact:</strong> {(string.IsNullOrEmpty(license.SupportContact) ? "N/A" : license.SupportContact)}</p>
                        </div>
                    ";

                await _emailBackgroundQueue.QueueEmailAsync(
                    new RenderedEmailMessage(toAddresses, subject, message),
                    cancellationToken);
                return true;
            }

            return false;
        }
    }
}
