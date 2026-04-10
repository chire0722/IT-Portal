using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.SignOnboardingRequest;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    public sealed class ProcessOnboardingSignatureCommandHandler
        : IRequestHandler<ProcessOnboardingSignatureCommand, CommandResponse>
    {
        private readonly ISender _sender;

        public ProcessOnboardingSignatureCommandHandler(ISender sender)
        {
            _sender = sender;
        }

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
