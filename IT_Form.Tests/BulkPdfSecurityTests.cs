// Project comment: BulkPdfSecurityTests.cs is part of the IT Form application source code.

using IT_Form.Application.Features.Offboarding.ExportBulkOffboardingPdf;
using IT_Form.Application.Features.Onboarding.ExportBulkOnboardingPdf;

namespace IT_Form.Tests;

public sealed class BulkPdfSecurityTests
{
    [Fact]
    public void OnboardingBulkPdfValidator_RejectsOversizedSelection()
    {
        var result = new ExportBulkOnboardingPdfCommandValidator()
            .Validate(new ExportBulkOnboardingPdfCommand(Enumerable.Range(1, 101).ToArray()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("no more than 100", StringComparison.Ordinal));
    }

    [Fact]
    public void OffboardingBulkPdfValidator_RejectsOversizedSelection()
    {
        var result = new ExportBulkOffboardingPdfCommandValidator()
            .Validate(new ExportBulkOffboardingPdfCommand(Enumerable.Range(1, 101).ToArray()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("no more than 100", StringComparison.Ordinal));
    }
}

