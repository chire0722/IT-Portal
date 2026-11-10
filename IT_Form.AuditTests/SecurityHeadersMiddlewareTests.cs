using IT_Form.Security;
using Microsoft.AspNetCore.Http;

namespace IT_Form.AuditTests;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsRestrictiveCspWithoutHardcodedPrivateHttpOrigins()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.True(nextCalled);
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.Contains("base-uri 'self'", csp);
        Assert.Contains("form-action 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.DoesNotContain("172.16.", csp);
        Assert.DoesNotContain("http://", csp);
        Assert.DoesNotContain("'unsafe-eval'", csp);
        Assert.DoesNotContain("cdn.tailwindcss.com", csp);
    }


    [Fact]
    public async Task InvokeAsync_ScopesTailwindUnsafeEvalExceptionToVerificationRoutes()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/Verification/Index";
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.Contains("'unsafe-eval'", csp);
        Assert.Contains("https://cdn.tailwindcss.com", csp);
    }

    [Fact]
    public async Task InvokeAsync_AddsCoreSecurityHeaders()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());
    }
}
