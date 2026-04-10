using MediatR;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Common.Behaviors
{
    public sealed class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

        public RequestLoggingBehavior(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "MediatR request {RequestName} failed", requestName);
                throw;
            }
        }
    }
}
