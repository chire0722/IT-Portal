namespace IT_Form.Application.Features.Onboarding.ExportOnboardingCsv
{
    public sealed record ExportOnboardingCsvResult(
        byte[] Content,
        string FileName);
}
