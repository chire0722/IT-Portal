using System.Text.Json;
using IT_Form.Helpers;

namespace IT_Form.Tests;

public sealed class AuditPayloadSanitizerTests
{
    [Fact]
    public void SerializeSanitized_RedactsCredentialAndPersonalFields()
    {
        var payload = new
        {
            UserName = "employee@example.test",
            Authorization = "Bearer super-secret",
            ConnectionString = "Data Source=private.db",
            OtpCode = "123456",
            Department = "Information Technology"
        };

        var json = AuditPayloadSanitizer.SerializeSanitized(
            payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.DoesNotContain("employee@example.test", json);
        Assert.DoesNotContain("super-secret", json);
        Assert.DoesNotContain("private.db", json);
        Assert.DoesNotContain("123456", json);
        Assert.Contains("Information Technology", json);
        Assert.Contains("***REDACTED***", json);
    }
}
