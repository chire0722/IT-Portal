// Project comment: EmailConfirmationIntegrityTests.cs is part of the IT Form application source code.

using IT_Form.Helpers;
using IT_Form.Models;
using Microsoft.AspNetCore.DataProtection;

namespace IT_Form.Tests;

public sealed class EmailConfirmationIntegrityTests
{
    [Fact]
    public void Validate_RejectsTamperedServerGeneratedContext()
    {
        var provider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "IT_Form_EmailIntegrity", Guid.NewGuid().ToString("N"))));
        var integrity = new EmailConfirmationIntegrity(provider);
        var model = new EmailConfirmationViewModel
        {
            RequestId = 42,
            RequestType = "Onboarding",
            ConfirmationActorId = "actor-1",
            EmailType = "Onboarding",
            HasAttachment = false,
            BodyHtml = "<p>Approved content</p>",
            SignatureImage = null
        };

        model.BodyIntegrity = integrity.Create(model);
        Assert.True(integrity.Validate(model));

        model.BodyHtml = "<script>alert(1)</script>";
        Assert.False(integrity.Validate(model));
    }
}

