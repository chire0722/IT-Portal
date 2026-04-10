using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace IT_Form.AuditTests;

internal static class TestContextFactory
{
    public static (ApplicationDbContext Context, DefaultHttpContext HttpContext) Create(string? userName = "auditor@ytc.com")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var identity = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, userName)
            ], "Test");
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(identity);
        }

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string input) => input);
        encryption.Setup(x => x.DecryptKey(It.IsAny<string>())).Returns((string input) => input);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new ApplicationDbContext(
            options,
            accessor,
            Mock.Of<ILogger<ApplicationDbContext>>(),
            encryption.Object);

        return (context, httpContext);
    }
}
