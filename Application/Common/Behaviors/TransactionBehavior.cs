// Project comment: TransactionBehavior.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;

namespace IT_Form.Application.Common.Behaviors
{
    /// <summary>
    /// Defines the TransactionBehavior class used by the IT Form application.
    /// </summary>
    public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ApplicationDbContext _context;
        private readonly IPostCommitActionQueue _postCommitActionQueue;

        public TransactionBehavior(
            ApplicationDbContext context,
            IPostCommitActionQueue postCommitActionQueue)
        {
            _context = context;
            _postCommitActionQueue = postCommitActionQueue;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!ShouldWrapInTransaction(request))
            {
                try
                {
                    var response = await next();
                    if (_context.Database.CurrentTransaction == null)
                    {
                        await _postCommitActionQueue.FlushAsync(cancellationToken);
                    }

                    return response;
                }
                catch
                {
                    _postCommitActionQueue.Clear();
                    throw;
                }
            }

            if (_context.Database.CurrentTransaction != null)
            {
                try
                {
                    return await next();
                }
                catch
                {
                    _postCommitActionQueue.Clear();
                    throw;
                }
            }

            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await next();
                await transaction.CommitAsync(cancellationToken);
                await _postCommitActionQueue.FlushAsync(cancellationToken);
                return response;
            }
            catch
            {
                _postCommitActionQueue.Clear();
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Runs the ShouldWrapInTransaction operation.
        /// </summary>
        private static bool ShouldWrapInTransaction(TRequest request)
        {
            var requestTypeName = request.GetType().Name;

            if (!requestTypeName.EndsWith("Command", StringComparison.Ordinal))
            {
                return false;
            }

            // Import flows already manage explicit transactions inside their handlers.
            if (requestTypeName.Contains("Import", StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }
    }
}
