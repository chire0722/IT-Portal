// Project comment: OffboardingAutoSignService.cs is part of the IT Form application source code.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Data;
using IT_Form.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IT_Form.Services
{
    /// <summary>
    /// Provides OffboardingAutoSign service behavior for the application.
    /// </summary>
    public class OffboardingAutoSignService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OffboardingAutoSignService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Check every 6 hours

        public OffboardingAutoSignService(IServiceScopeFactory scopeFactory, ILogger<OffboardingAutoSignService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Runs the ExecuteAsync operation.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Offboarding Auto-Sign Background Service is starting.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessAutoSignAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Error occurred in Offboarding Auto-Sign Service.");
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the host is shutting down.
            }

            _logger.LogInformation("Offboarding Auto-Sign Background Service is stopping.");
        }

        /// <summary>
        /// Processes data for the ProcessAutoSignAsync operation.
        /// </summary>
        private async Task ProcessAutoSignAsync(CancellationToken stoppingToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Define 3 days threshold
                var thresholdDate = DateTime.UtcNow.AddDays(-3);

                // Find requests where:
                // 1. Manager hasn't signed yet
                // 2. Status is PendingHODApproval (the only valid status for manager auto-sign)
                // 3. HR has already signed more than 3 days ago
                var pendingRequests = await context.OffboardingRequests
                    .Where(r => r.HODSignedAt == null
                             && r.Status == RequestStatus.PendingHODApproval
                             && r.HRSignedAt.HasValue
                             && r.HRSignedAt.Value < thresholdDate)
                    .ToListAsync(stoppingToken);

                if (pendingRequests.Any())
                {
                    _logger.LogInformation($"Found {pendingRequests.Count} offboarding requests to auto-sign for managers.");

                    foreach (var request in pendingRequests)
                    {
                        request.HODSignature = "Auto-Signed by System";
                        request.HODSignedAt = DateTime.UtcNow;

                        // Check if IT has already signed (unlikely but possible)
                        if (!string.IsNullOrEmpty(request.ITAdminSignature) && !string.IsNullOrEmpty(request.HRSignature))
                        {
                            request.Status = RequestStatus.PendingEmployeeSign;
                            request.CompletedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            request.Status = RequestStatus.PendingITApproval;
                        }

                        request.UpdatedAt = DateTime.UtcNow;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
            }
        }
    }
}
