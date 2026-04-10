using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Services;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ExportBulkOnboardingPdf
{
    public sealed class ExportBulkOnboardingPdfCommandHandler : IRequestHandler<ExportBulkOnboardingPdfCommand, ProcessResult<BinaryFilePayload>>
    {
        private readonly IOnboardingDocumentService _onboardingDocumentService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ExportBulkOnboardingPdfCommandHandler(
            IOnboardingDocumentService onboardingDocumentService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _onboardingDocumentService = onboardingDocumentService;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportBulkOnboardingPdfCommand request, CancellationToken cancellationToken)
        {
            return await _onboardingDocumentService.ExportBulkPdfAsync(request.Ids, _currentUserAccessor.User, cancellationToken);
        }
    }
}
