// Project comment: OnboardingListingFlows.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.ExportBulkOnboardingPdf;
using IT_Form.Application.Features.Onboarding.ExportOnboardingCsv;
using IT_Form.Application.Features.Onboarding.GenerateOnboardingPdf;
using IT_Form.Application.Features.Onboarding.GetOnboardingIndex;
using IT_Form.Models.Enums;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingIndexPage query.
    /// </summary>
    public sealed record GetOnboardingIndexPageQuery(string? SearchString, RequestStatus? Status, int Page = 1)
        : IRequest<ProcessResult<GetOnboardingIndexResult>>;

    /// <summary>
    /// Handles the GetOnboardingIndexPageQuery query workflow.
    /// </summary>
    public sealed class GetOnboardingIndexPageQueryHandler
        : IRequestHandler<GetOnboardingIndexPageQuery, ProcessResult<GetOnboardingIndexResult>>
    {
        private readonly ISender _sender;

        public GetOnboardingIndexPageQueryHandler(ISender sender) => _sender = sender;

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<GetOnboardingIndexResult>> Handle(GetOnboardingIndexPageQuery request, CancellationToken cancellationToken) =>
            ProcessResult<GetOnboardingIndexResult>.Success(
                await _sender.Send(new GetOnboardingIndexQuery(request.SearchString, request.Status, request.Page), cancellationToken));
    }

    /// <summary>
    /// Carries data needed to execute the ExportOnboardingCsvFile query.
    /// </summary>
    public sealed record ExportOnboardingCsvFileQuery(string? SearchString, RequestStatus? Status)
        : IRequest<ProcessResult<BinaryFilePayload>>;

    /// <summary>
    /// Handles the ExportOnboardingCsvFileQuery query workflow.
    /// </summary>
    public sealed class ExportOnboardingCsvFileQueryHandler
        : IRequestHandler<ExportOnboardingCsvFileQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ISender _sender;

        public ExportOnboardingCsvFileQueryHandler(ISender sender) => _sender = sender;

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportOnboardingCsvFileQuery request, CancellationToken cancellationToken)
            => await _sender.Send(new ExportOnboardingCsvQuery(request.SearchString, request.Status), cancellationToken);
    }

    /// <summary>
    /// Carries data needed to execute the ExportBulkOnboardingPdfFile command.
    /// </summary>
    public sealed record ExportBulkOnboardingPdfFileCommand(int[] Ids)
        : IRequest<ProcessResult<BinaryFilePayload>>;

    /// <summary>
    /// Handles the ExportBulkOnboardingPdfFileCommand command workflow.
    /// </summary>
    public sealed class ExportBulkOnboardingPdfFileCommandHandler
        : IRequestHandler<ExportBulkOnboardingPdfFileCommand, ProcessResult<BinaryFilePayload>>
    {
        private readonly ISender _sender;

        public ExportBulkOnboardingPdfFileCommandHandler(ISender sender) => _sender = sender;

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportBulkOnboardingPdfFileCommand request, CancellationToken cancellationToken) =>
            await _sender.Send(new ExportBulkOnboardingPdfCommand(request.Ids), cancellationToken);
    }

    /// <summary>
    /// Carries data needed to execute the GenerateOnboardingPdfFile query.
    /// </summary>
    public sealed record GenerateOnboardingPdfFileQuery(int Id)
        : IRequest<ProcessResult<BinaryFilePayload>>;

    /// <summary>
    /// Handles the GenerateOnboardingPdfFileQuery query workflow.
    /// </summary>
    public sealed class GenerateOnboardingPdfFileQueryHandler
        : IRequestHandler<GenerateOnboardingPdfFileQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ISender _sender;

        public GenerateOnboardingPdfFileQueryHandler(ISender sender) => _sender = sender;

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(GenerateOnboardingPdfFileQuery request, CancellationToken cancellationToken) =>
            await _sender.Send(new GenerateOnboardingPdfQuery(request.Id), cancellationToken);
    }
}
