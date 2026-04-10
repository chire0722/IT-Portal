using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.ItSetup
{
    public sealed class StartOffboardingItSetupQueryHandler : IRequestHandler<StartOffboardingItSetupQuery, ProcessResult<object>>
    {
        private readonly ApplicationDbContext _context;

        public StartOffboardingItSetupQueryHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProcessResult<object>> Handle(StartOffboardingItSetupQuery request, CancellationToken cancellationToken)
        {
            var offboardingRequest = await _context.OffboardingRequests.FindAsync([request.Id], cancellationToken);
            if (offboardingRequest == null)
            {
                return ProcessResult<object>.NotFound();
            }

            if (!offboardingRequest.CanStartItSetup())
            {
                return ProcessResult<object>.Failure("This request is not awaiting IT setup.", "Details", request.Id);
            }

            return ProcessResult<object>.Success(message: string.Empty, redirectAction: "Step1", requestId: request.Id);
        }
    }
}
