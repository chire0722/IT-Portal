// Project comment: GenerateOnboardingPdfQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Services;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GenerateOnboardingPdf
{
    /// <summary>
    /// Handles the GenerateOnboardingPdfQuery query workflow.
    /// </summary>
    public sealed class GenerateOnboardingPdfQueryHandler : IRequestHandler<GenerateOnboardingPdfQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly IOnboardingDocumentService _onboardingDocumentService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GenerateOnboardingPdfQueryHandler(
            IOnboardingDocumentService onboardingDocumentService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _onboardingDocumentService = onboardingDocumentService;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(GenerateOnboardingPdfQuery request, CancellationToken cancellationToken)
        {
            return await _onboardingDocumentService.GeneratePdfAsync(request.Id, _currentUserAccessor.User, cancellationToken);
        }
    }
}
