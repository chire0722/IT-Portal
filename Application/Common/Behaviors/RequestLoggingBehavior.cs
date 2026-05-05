// Project comment: RequestLoggingBehavior.cs is part of the IT Form application source code.

using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Common.Behaviors
{
    /// <summary>
    /// Defines the RequestLoggingBehavior class used by the IT Form application.
    /// </summary>
    public sealed class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

        public RequestLoggingBehavior(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
        {
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
            var requestName = typeof(TRequest).Name;
            var isCommand = requestName.EndsWith("Command", StringComparison.Ordinal);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Handling MediatR request {RequestName}", requestName);
            }

            try
            {
                var response = await next();
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Handled MediatR request {RequestName}", requestName);
                }

                return response;
            }
            catch (ValidationException ex)
            {
                if (isCommand)
                {
                    _logger.LogInformation(
                        "Validation failed for command {CommandName}: {ValidationErrors}",
                        requestName,
                        string.Join("; ", ex.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}")));
                }
                else
                {
                    _logger.LogInformation(
                        "Validation failed for MediatR request {RequestName}: {ValidationErrors}",
                        requestName,
                        string.Join("; ", ex.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}")));
                }

                throw;
            }
            catch (Exception ex)
            {
                if (isCommand)
                {
                    _logger.LogError(ex, "FAILED COMMAND {CommandName}", requestName);
                }
                else
                {
                    _logger.LogError(ex, "MediatR request {RequestName} failed", requestName);
                }

                throw;
            }
        }
    }
}
