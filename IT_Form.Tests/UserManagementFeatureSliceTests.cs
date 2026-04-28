// Project comment: UserManagementFeatureSliceTests.cs is part of the IT Form application source code.

using System.Security.Claims;
using FluentValidation;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Features.UserManagement.Writes.CreateUserManagement;
using IT_Form.Application.Features.UserManagement.Writes.DeleteUserManagement;
using IT_Form.Application.Features.UserManagement.Writes.ToggleUserManagementActive;
using IT_Form.Application.Features.UserManagement.Writes.UpdateUserManagement;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the UserManagementFeatureSliceTests class used by the IT Form application.
/// </summary>
public class UserManagementFeatureSliceTests
{
    [Fact]
    /// <summary>
    /// Creates data for the CreateValidator_Fails_WhenUsernameAlreadyExists operation.
    /// </summary>
    public async Task CreateValidator_Fails_WhenUsernameAlreadyExists()
    {
        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByNameAsync("existing"))
            .ReturnsAsync(new ApplicationUser { Id = "u1", UserName = "existing", FullName = "Existing User" });

        var validator = new CreateUserManagementCommandValidator(userManager.Object);

        var result = await validator.ValidateAsync(new CreateUserManagementCommand(
            "New User",
            "existing",
            "new@test.local",
            "IT",
            "Password123!",
            "Password123!",
            true,
            false,
            false,
            true,
            ["Alpha"]));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateHandler_ThrowsNotFound_WhenUserMissing operation.
    /// </summary>
    public async Task UpdateHandler_ThrowsNotFound_WhenUserMissing()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager();
        var logger = Mock.Of<ILogger<UpdateUserManagementCommandHandler>>();
        var handler = new UpdateUserManagementCommandHandler(
            userManager.Object,
            context,
            new TestCurrentUserAccessor(),
            logger);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateUserManagementCommand(
                "missing",
                "Missing User",
                "missing",
                "missing@test.local",
                "IT",
                null,
                null,
                true,
                false,
                false,
                true,
                []), CancellationToken.None));
    }

    [Fact]
    /// <summary>
    /// Runs the ToggleHandler_TogglesUserActiveState operation.
    /// </summary>
    public async Task ToggleHandler_TogglesUserActiveState()
    {
        using var context = CreateContext();
        context.Users.Add(new ApplicationUser
        {
            Id = "u-toggle",
            UserName = "toggle",
            FullName = "Toggle User",
            Email = "toggle@test.local",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var handler = new ToggleUserManagementActiveCommandHandler(
            context,
            new TestCurrentUserAccessor(),
            Mock.Of<ILogger<ToggleUserManagementActiveCommandHandler>>());

        var result = await handler.Handle(new ToggleUserManagementActiveCommand("u-toggle"), CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.False((await context.Users.SingleAsync()).IsActive);
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteHandler_ThrowsValidation_WhenDeletingOwnAccount operation.
    /// </summary>
    public async Task DeleteHandler_ThrowsValidation_WhenDeletingOwnAccount()
    {
        using var context = CreateContext();
        context.Users.Add(new ApplicationUser
        {
            Id = "self",
            UserName = "self",
            FullName = "Self User",
            Email = "self@test.local",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var userManager = CreateUserManager();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Id = "self", UserName = "self", FullName = "Self User" });

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "self")], "mock"))
            });

        var handler = new DeleteUserManagementCommandHandler(
            context,
            userManager.Object,
            httpContextAccessor.Object,
            Mock.Of<ILogger<DeleteUserManagementCommandHandler>>());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new DeleteUserManagementCommand("self"), CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.ErrorMessage == "You cannot delete your own account.");
    }

    /// <summary>
    /// Creates data for the CreateContext operation.
    /// </summary>
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var http = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string s) => s);
        encryption.Setup(x => x.DecryptKey(It.IsAny<string>())).Returns((string s) => s);

        return new ApplicationDbContext(options, http.Object, logger.Object, encryption.Object);
    }

    /// <summary>
    /// Creates data for the CreateUserManager operation.
    /// </summary>
    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}
