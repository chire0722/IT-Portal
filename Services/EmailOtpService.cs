// Project comment: EmailOtpService.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using System.Text; // Added this namespace
using System.Security.Cryptography; // Added this namespace

namespace IT_Form.Services
{
    /// <summary>
    /// Provides IEmailOtp service behavior for the application.
    /// </summary>
    public interface IEmailOtpService
    {
        Task<string> GenerateOtpAsync(string email, string ipAddress, bool sendEmail = true);
        Task<bool> ValidateOtpAsync(string email, string code);
    }

    /// <summary>
    /// Provides EmailOtp service behavior for the application.
    /// </summary>
    public class EmailOtpService : IEmailOtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailOtpService> _logger;
        private readonly IOutlookService _outlookService;
        private readonly SecuritySettings _securitySettings;
        private readonly IDataProtector _otpProtector;

        public EmailOtpService(
            ApplicationDbContext context,
            ILogger<EmailOtpService> logger,
            IOutlookService outlookService,
            IDataProtectionProvider dataProtectionProvider,
            IOptions<SecuritySettings> securityOptions)
        {
            _context = context;
            _logger = logger;
            _outlookService = outlookService;
            _securitySettings = securityOptions.Value;
            _otpProtector = dataProtectionProvider.CreateProtector("IT_Form.EmailOtpService.Code");
        }

        /// <summary>
        /// Runs the GenerateOtpAsync operation.
        /// </summary>
        public async Task<string> GenerateOtpAsync(string email, string ipAddress, bool sendEmail = true)
        {
            // Reuse the MediatR pipeline transaction when one already exists.
            var ownsTransaction = _context.Database.CurrentTransaction == null;
            IDbContextTransaction? transaction = null;

            if (ownsTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                // 1. Invalidate any existing unused OTPs for this email using ExecuteUpdate for high performance
                await _context.OtpRecords
                    .Where(o => o.Email == email && !o.IsUsed)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsUsed, true));

                // 2. Generate a secure 6-digit OTP using the modern RandomNumberGenerator.GetInt32
                string code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

                var otpRecord = new OtpRecord
                {
                    Email = email,
                    Code = ProtectCode(code),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_securitySettings.OtpExpiryMinutes),
                    IsUsed = false,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OtpRecords.Add(otpRecord);
                await _context.SaveChangesAsync();

                // 3. Commit transaction
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                _logger.LogInformation("Atomic OTP generated and committed for {Email}", email);

                if (sendEmail)
                {
                    string subject = "IT Form - Password Recovery Code";
                    string body = $@"
                    <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 8px;'>
                        <h2 style='color: #2563eb;'>Password Recovery</h2>
                        <p>You requested a password reset. Please use the following code to verify your identity:</p>
                        <div style='background: #f3f4f6; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #111; margin: 20px 0;'>
                            {code}
                        </div>
                        <p style='color: #666; font-size: 14px;'>This code is for <b>one-time use</b> and will expire in <b>3 minutes</b>. If you did not request this, please ignore this email.</p>
                        <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;' />
                        <small>IT Asset Management System</small>
                    </div>";

                    _outlookService.DisplayEmail(email, subject, body);
                    _logger.LogInformation("OTP email sent to {Email}", email);
                }

                return code;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }

                _logger.LogError(ex, "Atomic OTP generation failed for {Email}", email);
                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Validates data for the ValidateOtpAsync operation.
        /// </summary>
        public async Task<bool> ValidateOtpAsync(string email, string code)
        {
            var otpRecord = await _context.OtpRecords
                .Where(o => o.Email == email && !o.IsUsed && !o.IsLocked)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Verification failed for {Email}: No active token.", email);
                return false;
            }

            // 3. Brute-force protection: check if max attempts reached
            if (otpRecord.FailedAttempts >= _securitySettings.MaxOtpAttempts || otpRecord.IsLocked)
            {
                otpRecord.IsLocked = true;
                otpRecord.IsUsed = true; // Permanent lock
                await _context.SaveChangesAsync();
                return false;
            }

            // 4. Validate code using Constant-Time Comparison (Hardened against Side-Channel/Timing attacks)
            var storedCode = UnprotectCodeOrFallback(otpRecord.Code);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(storedCode);
            byte[] providedBytes = Encoding.UTF8.GetBytes(code);

            if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
            {
                otpRecord.FailedAttempts += 1;
                if (otpRecord.FailedAttempts >= _securitySettings.MaxOtpAttempts)
                {
                    otpRecord.IsLocked = true;
                    otpRecord.IsUsed = true;
                }
                await _context.SaveChangesAsync();
                return false;
            }

            // 5. Success: Consume the code
            otpRecord.IsUsed = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("OTP validated successfully for {Email}", email);
            return true;
        }

        /// <summary>
        /// Runs the ProtectCode operation.
        /// </summary>
        private string ProtectCode(string code) => _otpProtector.Protect(code);

        /// <summary>
        /// Runs the UnprotectCodeOrFallback operation.
        /// </summary>
        private string UnprotectCodeOrFallback(string protectedOrPlainCode)
        {
            try
            {
                return _otpProtector.Unprotect(protectedOrPlainCode);
            }
            catch
            {
                // Backward compatibility for legacy rows created before at-rest protection.
                return protectedOrPlainCode;
            }
        }
    }
}
