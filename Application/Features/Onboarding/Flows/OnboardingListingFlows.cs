using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.ExportBulkOnboardingPdf;
using IT_Form.Application.Features.Onboarding.ExportOnboardingCsv;
using IT_Form.Application.Features.Onboarding.GenerateOnboardingPdf;
using IT_Form.Application.Features.Onboarding.GetOnboardingIndex;
using IT_Form.Models.Enums;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows
{
    public sealed record GetOnboardingIndexPageQuery(string? SearchString, RequestStatus? Status, int Page = 1)
        : IRequest<ProcessResult<GetOnboardingIndexResult>>;

    public sealed class GetOnboardingIndexPageQueryHandler
        : IRequestHandler<GetOnboardingIndexPageQuery, ProcessResult<GetOnboardingIndexResult>>
    {
        private readonly ISender _sender;

        public GetOnboardingIndexPageQueryHandler(ISender sender) => _sender = sender;

        public async Task<ProcessResult<GetOnboardingIndexResult>> Handle(GetOnboardingIndexPageQuery request, CancellationToken cancellationToken) =>
            ProcessResult<GetOnboardingIndexResult>.Success(
                await _sender.Send(new GetOnboardingIndexQuery(request.SearchString, request.Status, request.Page), cancellationToken));
    }

    public sealed record ExportOnboardingCsvFileQuery(string? SearchString, RequestStatus? Status)
        : IRequest<ProcessResult<BinaryFilePayload>>;

    public sealed class ExportOnboardingCsvFileQueryHandler
        : IRequestHandler<ExportOnboardingCsvFileQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ISender _sender;

        public ExportOnboardingCsvFileQueryHandler(ISender sender) => _sender = sender;

        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportOnboardingCsvFileQuery request, CancellationToken cancellationToken)
            => await _sender.Send(new ExportOnboardingCsvQuery(request.SearchString, request.Status), cancellationToken);
    }

    public sealed record ExportBulkOnboardingPdfFileCommand(int[] Ids)
        : IRequest<ProcessResult<BinaryFilePayload>>;

    public sealed class ExportBulkOnboardingPdfFileCommandHandler
        : IRequestHandler<ExportBulkOnboardingPdfFileCommand, ProcessResult<BinaryFilePayload>>
    {
        private readonly ISender _sender;

        public ExportBulkOnboardingPdfFileCommandHandler(ISender sender) => _sender = sender;

        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportBulkOnboardingPdfFileCommand request, CancellationToken cancellationToken) =>
            await _sender.Send(new ExportBulkOnboardingPdfCommand(request.Ids), cancellationToken);
    }

    public sealed record GenerateOnboardingPdfFileQuery(int Id)
        : IRequest<ProcessResult<BinaryFilePayload>>;

    public sealed class GenerateOnboardingPdfFileQueryHandler
        : IRequestHandler<GenerateOnboardingPdfFileQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ISender _sender;

        public GenerateOnboardingPdfFileQueryHandler(ISender sender) => _sender = sender;

        public async Task<ProcessResult<BinaryFilePayload>> Handle(GenerateOnboardingPdfFileQuery request, CancellationToken cancellationToken) =>
            await _sender.Send(new GenerateOnboardingPdfQuery(request.Id), cancellationToken);
    }
}
