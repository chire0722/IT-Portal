// Project comment: NullSender.cs is part of the IT Form application source code.

using MediatR;
using System.Collections.Generic;

namespace IT_Form.Services
{
    /// <summary>
    /// Defines the NullSender class used by the IT Form application.
    /// </summary>
    public sealed class NullSender : ISender
    {
        public static readonly NullSender Instance = new();

        private NullSender() { }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("ISender is not configured for this controller instance.");
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            throw new NotSupportedException("ISender is not configured for this controller instance.");
        }

        /// <summary>
        /// Runs the Send operation.
        /// </summary>
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("ISender is not configured for this controller instance.");
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("ISender is not configured for this controller instance.");
        }

        /// <summary>
        /// Creates data for the CreateStream operation.
        /// </summary>
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("ISender is not configured for this controller instance.");
        }
    }
}
