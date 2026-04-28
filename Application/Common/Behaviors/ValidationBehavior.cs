// Project comment: ValidationBehavior.cs is part of the IT Form application source code.

using FluentValidation;
using MediatR;

namespace IT_Form.Application.Common.Behaviors
{
    /// <summary>
    /// Defines the ValidationBehavior class used by the IT Form application.
    /// </summary>
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IValidator<TRequest>[] _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators as IValidator<TRequest>[] ?? validators.ToArray();
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (_validators.Length == 0)
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = results
                .SelectMany(r => r.Errors)
                .Where(e => e != null)
                .ToList();

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }

            return await next();
        }
    }
}
