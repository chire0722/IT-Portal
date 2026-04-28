// Project comment: ProcessOnboardingTokenRegenerationCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.RegenerateOnboardingToken;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    /// <summary>
    /// Handles the ProcessOnboardingTokenRegenerationCommand command workflow.
    /// </summary>
    public sealed class ProcessOnboardingTokenRegenerationCommandHandler
        : IRequestHandler<ProcessOnboardingTokenRegenerationCommand, CommandResponse>
    {
        private readonly ISender _sender;

        public ProcessOnboardingTokenRegenerationCommandHandler(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(ProcessOnboardingTokenRegenerationCommand request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RegenerateOnboardingTokenCommand(request.Id), cancellationToken);
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
