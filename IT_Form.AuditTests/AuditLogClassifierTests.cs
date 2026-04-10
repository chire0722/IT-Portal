using IT_Form.Helpers;
using IT_Form.Models;

namespace IT_Form.AuditTests;

public class AuditLogClassifierTests
{
    [Fact]
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
