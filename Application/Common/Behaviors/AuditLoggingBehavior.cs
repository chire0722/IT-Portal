// Project comment: AuditLoggingBehavior.cs is part of the IT Form application source code.

using System.Text.Json;
using FluentValidation;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Security;
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
            var httpContext = _httpContextAccessor.HttpContext;
            if (!ShouldAudit(request, httpContext))
            {
                return await next();
            }

            var action = request.GetType().Name;
            var userId = httpContext?.User?.Identity?.Name ?? "System";
            var payload = SerializePayload(request);
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
                var commandSucceeded = IsSuccessfulResponse(response);

                try
                {
                    await WriteSuccessAuditLogAsync(new AuditLog
                    {
                        UserId = userId,
                        Action = action,
                        Payload = payload,
                        CreatedAt = DateTime.UtcNow,
                        IsSuccess = commandSucceeded,
                        ErrorMessage = commandSucceeded ? null : "Command returned an unsuccessful result.",
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
                        ErrorMessage = "Command failed; detailed exception is recorded in application logs.",
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

                if (ex is ValidationException validationException)
                {
                    _logger.LogInformation(
                        "Validation failed for command {CommandName} by user {UserId}: {ValidationErrors}",
                        action,
                        userId,
                        string.Join("; ", validationException.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}")));
                }
                else
                {
                    _logger.LogError(ex, "Command {CommandName} failed for user {UserId}", action, userId);
                }

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

        private static bool IsSuccessfulResponse(TResponse response)
        {
            if (response == null)
            {
                return true;
            }

            var responseType = response.GetType();
            foreach (var propertyName in new[] { "IsSuccess", "IsAuthenticated", "IsVerified" })
            {
                var property = responseType.GetProperty(propertyName);
                if (property?.PropertyType == typeof(bool) && property.GetValue(response) is bool value)
                {
                    return value;
                }
            }

            return true;
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
        private static bool ShouldAudit(TRequest request, HttpContext? httpContext)
        {
            if (!request.GetType().Name.EndsWith("Command", StringComparison.Ordinal))
            {
                return false;
            }

            // Authentication OTP commands are audited as security events too.
            // AuditPayloadSanitizer removes OTP values and account identifiers from payloads.
            return !string.Equals(
                request.GetType().Name,
                "LoginAccountCommand",
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Runs the SerializePayload operation.
        /// </summary>
        private static string SerializePayload(TRequest request)
        {
            try
            {
                return AuditPayloadSanitizer.SerializeSanitized(request, SerializerOptions);
            }
            catch (Exception)
            {
                return JsonSerializer.Serialize(new
                {
                    Error = "Payload serialization failed.",
                    Exception = "Payload serialization failed; sensitive exception details are intentionally omitted.",
                    RequestType = request.GetType().FullName
                }, SerializerOptions);
            }
        }
    }
}
