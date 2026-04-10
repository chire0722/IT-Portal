using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace IT_Form.Application.Common.Behaviors
{
    public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private static readonly TimeSpan QueryWarningThreshold = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan CommandWarningThreshold = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan DefaultWarningThreshold = TimeSpan.FromSeconds(2);
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

        public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await next();
            stopwatch.Stop();

            var elapsed = stopwatch.Elapsed;
            var requestName = typeof(TRequest).Name;
            var warningThreshold = ResolveWarningThreshold(requestName);

            if (elapsed >= warningThreshold)
            {
                _logger.LogWarning(
                    "Slow MediatR request {RequestName} took {ElapsedMilliseconds}ms (threshold {ThresholdMilliseconds}ms)",
                    requestName,
                    elapsed.TotalMilliseconds,
                    warningThreshold.TotalMilliseconds);
            }
            else if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Handled MediatR request {RequestName} in {ElapsedMilliseconds}ms",
                    requestName,
                    elapsed.TotalMilliseconds);
            }

            return response;
        }

        private static TimeSpan ResolveWarningThreshold(string requestName)
        {
            if (requestName.EndsWith("Command", StringComparison.Ordinal))
            {
                return CommandWarningThreshold;
            }

            if (requestName.EndsWith("Query", StringComparison.Ordinal))
            {
                return QueryWarningThreshold;
            }

            return DefaultWarningThreshold;
        }
    }
}
