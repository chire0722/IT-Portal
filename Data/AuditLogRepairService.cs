using Microsoft.EntityFrameworkCore;

namespace IT_Form.Data;

public sealed class AuditLogRepairService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogRepairService> _logger;

    public AuditLogRepairService(
        ApplicationDbContext context,
        ILogger<AuditLogRepairService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RepairAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var repairedOutcomeCount = await _context.Database.ExecuteSqlRawAsync(
            """
            UPDATE AuditLogs
            SET IsSuccess = 1
            WHERE IsSuccess = 0
              AND (ErrorMessage IS NULL OR trim(ErrorMessage) = '')
              AND substr(CreatedAt, 1, 10) = '0001-01-01';
            """,
            cancellationToken);

        var repairedTimestampCount = await _context.Database.ExecuteSqlRawAsync(
            """
            UPDATE AuditLogs
            SET CreatedAt = Timestamp
            WHERE substr(CreatedAt, 1, 10) = '0001-01-01'
              AND Timestamp IS NOT NULL
              AND trim(Timestamp) <> '';
            """,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        if (repairedOutcomeCount > 0 || repairedTimestampCount > 0)
        {
            _logger.LogInformation(
                "Repaired audit log history. Normalized outcome on {OutcomeCount} row(s) and timestamp on {TimestampCount} row(s).",
                repairedOutcomeCount,
                repairedTimestampCount);
        }
    }
}
