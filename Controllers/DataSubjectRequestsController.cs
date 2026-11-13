using System.Security.Claims;
using IT_Form.Data;
using IT_Form.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Controllers;

[ApiController]
[Route("api/data-subject-requests")]
[Authorize(Policy = "RequireHR")]
[EnableRateLimiting("strict")]
public sealed class DataSubjectRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DataSubjectRequestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([FromBody] SubmitDataSubjectRequestModel model, CancellationToken cancellationToken)
    {
        var actor = CurrentUserId();
        if (string.IsNullOrWhiteSpace(actor))
            return Unauthorized();

        if (!Enum.IsDefined(model.RequestType) || string.IsNullOrWhiteSpace(model.IdempotencyKey) ||
            model.IdempotencyKey.Length is < 8 or > 128 ||
            (!string.IsNullOrWhiteSpace(model.CompanyScope) && model.CompanyScope.Length > 256))
            return BadRequest("A valid request type and idempotency key are required.");

        var existing = await _context.DataSubjectRequests
            .SingleOrDefaultAsync(
                request => request.SubmittedByUserId == actor && request.IdempotencyKey == model.IdempotencyKey,
                cancellationToken);
        if (existing != null)
        {
            if (existing.RequestType != model.RequestType)
                return Conflict("The idempotency key is already bound to a different request type.");
            return Ok(ToResponse(existing));
        }

        var request = new DataSubjectRequest
        {
            RequestType = model.RequestType,
            SubjectUserId = actor,
            SubmittedByUserId = actor,
            CompanyScope = string.IsNullOrWhiteSpace(model.CompanyScope) ? null : model.CompanyScope.Trim(),
            IdempotencyKey = model.IdempotencyKey.Trim(),
            Events =
            {
                new DataSubjectRequestEvent
                {
                    FromStatus = DataSubjectRequestStatus.Submitted,
                    ToStatus = DataSubjectRequestStatus.Submitted,
                    ActorUserId = actor,
                    Reason = "Request submitted."
                }
            }
        };

        _context.DataSubjectRequests.Add(request);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A unique idempotency constraint makes retries safe even when two requests race.
            var raced = await _context.DataSubjectRequests.SingleOrDefaultAsync(
                item => item.SubmittedByUserId == actor && item.IdempotencyKey == model.IdempotencyKey,
                cancellationToken);
            if (raced == null)
                throw;
            return Ok(ToResponse(raced));
        }

        return CreatedAtAction(nameof(Get), new { id = request.Id }, ToResponse(request));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var requests = await _context.DataSubjectRequests
            .AsNoTracking()
            .OrderByDescending(request => request.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
        return Ok(requests.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var request = await _context.DataSubjectRequests.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        return request == null ? NotFound() : Ok(ToResponse(request));
    }

    [HttpPost("{id:int}/transition")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transition(
        int id,
        [FromBody] TransitionDataSubjectRequestModel model,
        CancellationToken cancellationToken)
    {
        var actor = CurrentUserId();
        if (string.IsNullOrWhiteSpace(actor))
            return Unauthorized();

        var request = await _context.DataSubjectRequests.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request == null)
            return NotFound();

        if (!Enum.IsDefined(model.Status) || !IsAllowedTransition(request.Status, model.Status))
            return BadRequest("The requested data-subject request state transition is not allowed.");

        if (model.Status == DataSubjectRequestStatus.Completed &&
            (string.IsNullOrWhiteSpace(model.CompletionEvidenceReference) || model.CompletionEvidenceReference.Length > 256))
            return BadRequest("Completion requires a non-sensitive evidence reference.");
        if (model.Reason?.Length > 2000)
            return BadRequest("The transition reason is too long.");

        var from = request.Status;
        request.Status = model.Status;
        request.AssignedToUserId = actor;
        request.DecisionReason = string.IsNullOrWhiteSpace(model.Reason) ? null : model.Reason.Trim();
        request.CompletionEvidenceReference = model.Status == DataSubjectRequestStatus.Completed
            ? model.CompletionEvidenceReference!.Trim()
            : request.CompletionEvidenceReference;
        request.CompletedAtUtc = model.Status == DataSubjectRequestStatus.Completed ? DateTime.UtcNow : null;
        request.UpdatedAtUtc = DateTime.UtcNow;
        request.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        request.Events.Add(new DataSubjectRequestEvent
        {
            FromStatus = from,
            ToStatus = model.Status,
            ActorUserId = actor,
            Reason = request.DecisionReason
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The request was changed by another operator. Reload it before retrying.");
        }

        return Ok(ToResponse(request));
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;

    private static bool IsAllowedTransition(DataSubjectRequestStatus from, DataSubjectRequestStatus to) =>
        (from, to) switch
        {
            (DataSubjectRequestStatus.Submitted, DataSubjectRequestStatus.IdentityVerificationRequired) => true,
            (DataSubjectRequestStatus.Submitted, DataSubjectRequestStatus.Cancelled) => true,
            (DataSubjectRequestStatus.IdentityVerificationRequired, DataSubjectRequestStatus.InReview) => true,
            (DataSubjectRequestStatus.IdentityVerificationRequired, DataSubjectRequestStatus.Rejected) => true,
            (DataSubjectRequestStatus.IdentityVerificationRequired, DataSubjectRequestStatus.Cancelled) => true,
            (DataSubjectRequestStatus.InReview, DataSubjectRequestStatus.Approved) => true,
            (DataSubjectRequestStatus.InReview, DataSubjectRequestStatus.Rejected) => true,
            (DataSubjectRequestStatus.InReview, DataSubjectRequestStatus.Cancelled) => true,
            (DataSubjectRequestStatus.Approved, DataSubjectRequestStatus.Completed) => true,
            (DataSubjectRequestStatus.Approved, DataSubjectRequestStatus.Cancelled) => true,
            _ => false
        };

    private static DataSubjectRequestResponse ToResponse(DataSubjectRequest request) => new(
        request.Id,
        request.RequestReference,
        request.RequestType,
        request.Status,
        request.SubmittedAtUtc,
        request.UpdatedAtUtc,
        request.CompletedAtUtc,
        request.AssignedToUserId != null,
        request.CompletionEvidenceReference != null);
}

public sealed record SubmitDataSubjectRequestModel(
    DataSubjectRequestType RequestType,
    string IdempotencyKey,
    string? CompanyScope);

public sealed record TransitionDataSubjectRequestModel(
    DataSubjectRequestStatus Status,
    string? Reason,
    string? CompletionEvidenceReference);

public sealed record DataSubjectRequestResponse(
    int Id,
    string RequestReference,
    DataSubjectRequestType RequestType,
    DataSubjectRequestStatus Status,
    DateTime SubmittedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    bool Assigned,
    bool HasCompletionEvidence);
