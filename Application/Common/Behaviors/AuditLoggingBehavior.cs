// Project comment: AuditLoggingBehavior.cs is part of the IT Form application source code.

using System.Text.Json;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Common.Behaviors
{
    /// <summary>
    /// Defines the AuditLoggingBehavior class used by the IT Form application.
    /// </summary>
    public sealed class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private const string CorrelationIdItemKey = "AuditCorrelationId";
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceScopeFactory? _scopeFactory;
        private readonly ILogger<AuditLoggingBehavior<TRequest, TResponse>> _logger;

        public AuditLoggingBehavior(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IServiceScopeFactory? scopeFactory,
            ILogger<AuditLoggingBehavior<TRequest, TResponse>> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!ShouldAudit(request))
            {
                return await next();
            }

            var action = request.GetType().Name;
            var userId = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
            var payload = SerializePayload(request);
            var httpContext = _httpContextAccessor.HttpContext;
            var hadExistingCorrelation = httpContext?.Items.ContainsKey(CorrelationIdItemKey) == true;
            var previousCorrelationValue = hadExistingCorrelation ? httpContext!.Items[CorrelationIdItemKey] : null;
            var correlationId = previousCorrelationValue is Guid existingCorrelation
                ? existingCorrelation
                : Guid.NewGuid();

            if (httpContext != null)
            {
                httpContext.Items[CorrelationIdItemKey] = correlationId;
            }

            try
            {
                var response = await next();

                try
                {
                    await WriteSuccessAuditLogAsync(new AuditLog
                    {
                        UserId = userId,
                        Action = action,
                        Payload = payload,
                        CreatedAt = DateTime.UtcNow,
                        IsSuccess = true,
                        ErrorMessage = null,
                        Timestamp = DateTime.UtcNow,
                        EntityName = "Command",
                        EntityId = "-",
                        PerformedBy = userId,
                        Details = action,
                        Changes = payload,
                        IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                        CorrelationId = correlationId
                    }, cancellationToken);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to persist success audit log for command {CommandName}", action);
                }

                return response;
            }
            catch (Exception ex)
            {
                try
                {
                    await WriteFailureAuditLogAsync(new AuditLog
                    {
                        UserId = userId,
                        Action = action,
                        Payload = payload,
                        CreatedAt = DateTime.UtcNow,
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                        Timestamp = DateTime.UtcNow,
                        EntityName = "Command",
                        EntityId = "-",
                        PerformedBy = userId,
                        Details = action,
                        Changes = payload,
                        IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                        CorrelationId = correlationId
                    }, cancellationToken);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to persist audit log for command {CommandName}", action);
                }

                _logger.LogError(ex, "Command {CommandName} failed for user {UserId}", action, userId);
                throw;
            }
            finally
            {
                if (httpContext != null)
                {
                    if (hadExistingCorrelation)
                    {
                        httpContext.Items[CorrelationIdItemKey] = previousCorrelationValue!;
                    }
                    else
                    {
                        httpContext.Items.Remove(CorrelationIdItemKey);
                    }
                }
            }
        }

        /// <summary>
        /// Runs the WriteSuccessAuditLogAsync operation.
        /// </summary>
        private async Task WriteSuccessAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken)
        {
            // Use the current request DbContext/connection so SQLite does not deadlock
            // behind a second writer trying to persist the command audit.
            await _context.PersistStandaloneAuditLogAsync(auditLog, cancellationToken);
        }

        /// <summary>
        /// Runs the WriteFailureAuditLogAsync operation.
        /// </summary>
        private async Task WriteFailureAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken)
        {
            if (_scopeFactory == null)
            {
                await _context.PersistStandaloneAuditLogAsync(auditLog, cancellationToken);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var auditContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await auditContext.PersistStandaloneAuditLogAsync(auditLog, cancellationToken);
        }

        /// <summary>
        /// Runs the ShouldAudit operation.
        /// </summary>
        private static bool ShouldAudit(TRequest request)
            => request.GetType().Name.EndsWith("Command", StringComparison.Ordinal);

        /// <summary>
        /// Runs the SerializePayload operation.
        /// </summary>
        private static string SerializePayload(TRequest request)
        {
            try
            {
                return AuditPayloadSanitizer.SerializeSanitized(request, SerializerOptions);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    Error = "Payload serialization failed.",
                    Exception = ex.Message,
                    RequestType = request.GetType().FullName
                }, SerializerOptions);
            }
        }
    }
}
