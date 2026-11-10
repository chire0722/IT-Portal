using IT_Form.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;

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
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp);
        Assert.Contains("script-src-attr 'none'", csp);
        Assert.Matches("script-src 'self' 'nonce-[A-Za-z0-9+/=]+", csp);
        Assert.DoesNotContain("cdn.tailwindcss.com", csp);
    }


    [Fact]
    public async Task InvokeAsync_DoesNotGrantVerificationRouteScriptExceptions()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/Verification/Index";
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.DoesNotContain("'unsafe-eval'", csp);
        Assert.DoesNotContain("https://cdn.tailwindcss.com", csp);
        Assert.Contains("script-src-attr 'none'", csp);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp);
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

    [Fact]
    public async Task CspNonceTagHelper_AddsRequestNonceToScriptElement()
    {
        var context = new TagHelperContext(
            tagName: "script",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object?>
            {
                [SecurityHeadersMiddleware.CspNonceItemKey] = "nonce-value"
            },
            uniqueId: "script-1");
        var output = new TagHelperOutput(
            "script",
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        await new CspNonceTagHelper().ProcessAsync(context, output);

        Assert.Equal("nonce-value", output.Attributes["nonce"]?.Value);
    }
}

