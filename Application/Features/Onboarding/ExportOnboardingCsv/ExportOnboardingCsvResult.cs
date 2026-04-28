// Project comment: ExportOnboardingCsvResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.ExportOnboardingCsv
{
    /// <summary>
    /// Represents the result returned by the ExportOnboardingCsv workflow.
    /// </summary>
    public sealed record ExportOnboardingCsvResult(
        byte[] Content,
        string FileName);
}
