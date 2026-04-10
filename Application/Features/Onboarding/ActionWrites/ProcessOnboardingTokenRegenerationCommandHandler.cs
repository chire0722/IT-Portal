using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.RegenerateOnboardingToken;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    public sealed class ProcessOnboardingTokenRegenerationCommandHandler
        : IRequestHandler<ProcessOnboardingTokenRegenerationCommand, CommandResponse>
    {
        private readonly ISender _sender;

        public ProcessOnboardingTokenRegenerationCommandHandler(ISender sender)
        {
            _sender = sender;
        }

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
