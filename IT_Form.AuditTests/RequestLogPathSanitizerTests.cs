using IT_Form.Security;
using Microsoft.AspNetCore.Http;

namespace IT_Form.AuditTests;

public sealed class RequestLogPathSanitizerTests
{
    [Theory]
    [InlineData("/verify/employee-device-bearer-token", "/verify/[REDACTED]")]
    [InlineData("/api/inventory/status/device-token", "/api/inventory/status/[REDACTED]")]
    [InlineData("/api/inventory/download/device-token", "/api/inventory/download/[REDACTED]")]
    [InlineData("/api/inventory/generate-token/EMP-00123", "/api/inventory/generate-token/[REDACTED]")]
    public void Sanitize_RedactsKnownSensitiveRouteSegments(string path, string expected)
    {
        Assert.Equal(expected, RequestLogPathSanitizer.Sanitize(new PathString(path)));
    }

    [Fact]
    public void Sanitize_RedactsGuidAndLongOpaqueSegments()
    {
        var guid = Guid.NewGuid().ToString("D");
        var opaqueToken = "0123456789abcdef0123456789abcdef";

        Assert.Equal("/api/request/[REDACTED]", RequestLogPathSanitizer.Sanitize(new PathString($"/api/request/{guid}")));
        Assert.Equal("/api/request/[REDACTED]", RequestLogPathSanitizer.Sanitize(new PathString($"/api/request/{opaqueToken}")));
    }

    [Fact]
    public void Sanitize_LeavesOrdinaryRoutePathsUnchanged()
    {
        const string path = "/Home/Index";

        Assert.Equal(path, RequestLogPathSanitizer.Sanitize(new PathString(path)));
    }
}
