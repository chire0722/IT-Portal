// Project comment: ProcessOffboardingTokenRegenerationCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Offboarding.RegenerateOffboardingToken;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Handles the ProcessOffboardingTokenRegenerationCommand command workflow.
    /// </summary>
    public sealed class ProcessOffboardingTokenRegenerationCommandHandler : IRequestHandler<ProcessOffboardingTokenRegenerationCommand, CommandResponse>
    {
        private readonly ISender _sender;
        public ProcessOffboardingTokenRegenerationCommandHandler(ISender sender) => _sender = sender;

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(ProcessOffboardingTokenRegenerationCommand request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RegenerateOffboardingTokenCommand(request.Id), cancellationToken);
            return new CommandResponse
            {
                IsSuccess = result.IsSuccess,
                IsNotFound = result.IsNotFound,
                IsForbidden = result.IsForbidden,
                Message = result.Message,
                RequestId = result.RequestId > 0 ? result.RequestId : request.Id
            };
        }
    }
}
