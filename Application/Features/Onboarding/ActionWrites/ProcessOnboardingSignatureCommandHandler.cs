// Project comment: ProcessOnboardingSignatureCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.SignOnboardingRequest;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    /// <summary>
    /// Handles the ProcessOnboardingSignatureCommand command workflow.
    /// </summary>
    public sealed class ProcessOnboardingSignatureCommandHandler
        : IRequestHandler<ProcessOnboardingSignatureCommand, CommandResponse>
    {
        private readonly ISender _sender;

        public ProcessOnboardingSignatureCommandHandler(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<CommandResponse> Handle(ProcessOnboardingSignatureCommand request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new SignOnboardingRequestCommand(request.Id, request.SignatureType, request.SignatureData),
                cancellationToken);

            return result.Status switch
            {
                SignOnboardingRequestStatus.NotFound => new CommandResponse
                {
                    IsNotFound = true,
                    Message = result.Message,
                    RequestId = request.Id
                },
                SignOnboardingRequestStatus.ForbiddenScope => new CommandResponse
                {
                    IsForbidden = true,
                    Message = result.Message,
                    RequestId = result.RequestId
                },
                SignOnboardingRequestStatus.Signed => new CommandResponse
                {
                    IsSuccess = true,
                    Message = result.Message,
                    RequestId = result.RequestId
                },
                _ => new CommandResponse
                {
                    IsSuccess = false,
                    Message = result.Message,
                    RequestId = result.RequestId
                }
            };
        }
    }
}
