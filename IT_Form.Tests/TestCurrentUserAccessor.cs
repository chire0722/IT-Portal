using System.Security.Claims;
using IT_Form.Application.Common.Abstractions;

namespace IT_Form.Tests;

public sealed class TestCurrentUserAccessor : ICurrentUserAccessor
{
    public TestCurrentUserAccessor(
        string? userName = "test.user",
        string? userId = "test-user-id",
        IEnumerable<string>? titles = null,
        IEnumerable<string>? companies = null)
    {
        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(userName))
        {
            claims.Add(new Claim(ClaimTypes.Name, userName));
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        foreach (var title in titles ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                claims.Add(new Claim("UserTitle", title));
            }
        }

        foreach (var company in companies ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(company))
            {
                claims.Add(new Claim("UserCompany", company));
            }
        }

        User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    public ClaimsPrincipal User { get; }

    public string? UserName => User.Identity?.Name;
}
