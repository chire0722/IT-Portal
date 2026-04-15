using System.Security.Claims;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class UserTitleClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_AddsUserCompanyClaims_WhenUserTitleAlreadyExists()
    {
        await using var context = CreateContext();
        context.Users.Add(new ApplicationUser
        {
            Id = "user-1",
            UserName = "it.user",
            FullName = "IT User",
            EmailConfirmed = true
        });
        context.UserTitles.Add(new UserTitle { UserId = "user-1", Title = "IT" });
        context.UserCompanies.AddRange(
            new UserCompany { UserId = "user-1", CompanyName = "Alpha" },
            new UserCompany { UserId = "user-1", CompanyName = "Beta" });
        await context.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new UserTitleClaimsTransformation(context, cache);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Name, "it.user"),
            new Claim("UserTitle", "IT")
        ], "test"));

        var transformed = await service.TransformAsync(principal);
        var companies = transformed.FindAll("UserCompany").Select(claim => claim.Value).ToList();
        var titles = transformed.FindAll("UserTitle").Select(claim => claim.Value).ToList();

        Assert.Equal(2, companies.Count);
        Assert.Contains("Alpha", companies);
        Assert.Contains("Beta", companies);
        Assert.Single(titles);
        Assert.Equal("IT", titles[0]);
    }

    [Fact]
    public async Task TransformAsync_RemovesStaleMustChangePasswordClaim_WhenFlagIsCleared()
    {
        await using var context = CreateContext();
        context.Users.Add(new ApplicationUser
        {
            Id = "user-2",
            UserName = "staff.user",
            FullName = "Staff User",
            EmailConfirmed = true,
            MustChangePassword = false
        });
        await context.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new UserTitleClaimsTransformation(context, cache);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-2"),
            new Claim(ClaimTypes.Name, "staff.user"),
            new Claim("MustChangePassword", "true")
        ], "test"));

        var transformed = await service.TransformAsync(principal);

        Assert.DoesNotContain(transformed.Claims, claim => claim.Type == "MustChangePassword");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(service => service.EncryptKey(It.IsAny<string>())).Returns((string value) => value);
        encryption.Setup(service => service.DecryptKey(It.IsAny<string>())).Returns((string value) => value);

        return new ApplicationDbContext(options, httpContextAccessor.Object, logger.Object, encryption.Object);
    }
}
