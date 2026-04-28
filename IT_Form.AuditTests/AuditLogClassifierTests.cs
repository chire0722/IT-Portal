// Project comment: AuditLogClassifierTests.cs is part of the IT Form application source code.

using IT_Form.Helpers;
using IT_Form.Models;

namespace IT_Form.AuditTests;

/// <summary>
/// Defines the AuditLogClassifierTests class used by the IT Form application.
/// </summary>
public class AuditLogClassifierTests
{
    [Fact]
    /// <summary>
    /// Gets the data required by the GetSourceType_ReturnsCommandPipeline_ForCommandAudits operation.
    /// </summary>
    public void GetSourceType_ReturnsCommandPipeline_ForCommandAudits()
    {
        var log = new AuditLog
        {
            EntityName = "Command",
            Action = "SubmitOffboardingCreateCommand"
        };

        Assert.Equal("Command Pipeline", AuditLogClassifier.GetSourceType(log));
        Assert.Equal("Business Command", AuditLogClassifier.GetStageLabel(log));
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetSourceType_ReturnsSecurityEvent_ForUserAccountAudits operation.
    /// </summary>
    public void GetSourceType_ReturnsSecurityEvent_ForUserAccountAudits()
    {
        var log = new AuditLog
        {
            EntityName = "User Account",
            Action = "Login"
        };

        Assert.Equal("Security Event", AuditLogClassifier.GetSourceType(log));
        Assert.Equal("Authentication", AuditLogClassifier.GetStageLabel(log));
    }
}
