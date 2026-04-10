using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Step2
{
    public sealed class GetOffboardingStep2QueryHandler : IRequestHandler<GetOffboardingStep2Query, ProcessResult<OffboardingRequest>>
    {
        private readonly ApplicationDbContext _context;

        public GetOffboardingStep2QueryHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProcessResult<OffboardingRequest>> Handle(GetOffboardingStep2Query request, CancellationToken cancellationToken)
        {
            var offboardingRequest = await _context.OffboardingRequests.FindAsync([request.Id], cancellationToken);
            if (offboardingRequest == null)
            {
                return ProcessResult<OffboardingRequest>.NotFound();
            }

            if (offboardingRequest.Status != RequestStatus.PendingITApproval)
            {
                return ProcessResult<OffboardingRequest>.Failure("This request is not awaiting IT setup.", "Details", request.Id);
            }

            return ProcessResult<OffboardingRequest>.Success(offboardingRequest);
        }
    }
}
