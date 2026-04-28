// Project comment: ProcessOffboardingRejectionCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Offboarding.RejectOffboardingRequest;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Handles the ProcessOffboardingRejectionCommand command workflow.
    /// </summary>
    public sealed class ProcessOffboardingRejectionCommandHandler : IRequestHandler<ProcessOffboardingRejectionCommand, CommandResponse>
    {
        private readonly ISender _sender;
        public ProcessOffboardingRejectionCommandHandler(ISender sender) => _sender = sender;

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(ProcessOffboardingRejectionCommand request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RejectOffboardingRequestCommand(request.Id, request.Reason), cancellationToken);
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
