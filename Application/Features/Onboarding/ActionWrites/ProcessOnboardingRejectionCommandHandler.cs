// Project comment: ProcessOnboardingRejectionCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.RejectOnboardingRequest;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    /// <summary>
    /// Handles the ProcessOnboardingRejectionCommand command workflow.
    /// </summary>
    public sealed class ProcessOnboardingRejectionCommandHandler
        : IRequestHandler<ProcessOnboardingRejectionCommand, CommandResponse>
    {
        private readonly ISender _sender;

        public ProcessOnboardingRejectionCommandHandler(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(ProcessOnboardingRejectionCommand request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RejectOnboardingRequestCommand(request.Id, request.Reason), cancellationToken);
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
