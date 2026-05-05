// Project comment: AccountFeatureSliceTests.cs is part of the IT Form application source code.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Common.Support;
using IT_Form.Application.Features.Account;
using IT_Form.Application.Features.Account.ForgotPassword;
using IT_Form.Application.Features.Account.LoginAccount;
using IT_Form.Application.Features.Account.Queries.GetLoginPageBranding;
using IT_Form.Application.Features.Account.Queries.GetProfilePage;
using IT_Form.Application.Features.Account.Queries.GetVerifyOtpPage;
using IT_Form.Application.Features.Account.ResetPassword;
using IT_Form.Application.Features.Account.VerifyOtp;
using IT_Form.Application.Features.Account.Writes.ForceChangePassword;
using IT_Form.Application.Features.Account.Writes.Logout;
using IT_Form.Application.Features.Account.Writes.ProfilePasswordChange;
using IT_Form.Application.Features.Account.Writes.ResendOtp;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Security;
using IT_Form.Services;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the AccountFeatureSliceTests class used by the IT Form application.
/// </summary>
public class AccountFeatureSliceTests
{
    public AccountFeatureSliceTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Runs the LoginValidator_Fails_WhenUsernameMissing operation.
    /// </summary>
    public void LoginValidator_Fails_WhenUsernameMissing()
    {
        var validator = new LoginAccountCommandValidator();

        var result = validator.Validate(new LoginAccountCommand(string.Empty, "password", false, "127.0.0.1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the LoginHandler_ReturnsInvalidCredentials_WhenUserMissing operation.
    /// </summary>
    public async Task LoginHandler_ReturnsInvalidCredentials_WhenUserMissing()
    {
        using var context = CreateContext();

        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var queue = new Mock<IEmailBackgroundQueue>();
        var logger = Mock.Of<ILogger<LoginAccountCommandHandler>>();
        var postCommitQueue = CreatePostCommitActionQueue();
        var securityOptions = Options.Create(new SecuritySettings { MaxOtpAttempts = 3 });

        var handler = new LoginAccountCommandHandler(
            userManager.Object,
            signInManager.Object,
            context,
            queue.Object,
            postCommitQueue,
            new KeyedLockProvider(),
            securityOptions,
            logger);

        var result = await handler.Handle(
            new LoginAccountCommand("missing-user", "wrong-password", false, "127.0.0.1"),
            CancellationToken.None);

        Assert.False(result.IsAuthenticated);
        Assert.Equal("Invalid username/email or password.", result.Message);
    }

    [Fact]
    /// <summary>
    /// Runs the LoginHandler_QueuesLockoutAlert_WhenThresholdReached operation.
    /// </summary>
    public async Task LoginHandler_QueuesLockoutAlert_WhenThresholdReached()
    {
        using var context = CreateContext();

        var userManager = CreateUserManager();
        userManager.Object.PasswordHasher = new PasswordHasher<ApplicationUser>();
        var signInManager = CreateSignInManager(userManager.Object);
        var queue = new Mock<IEmailBackgroundQueue>();
        var logger = Mock.Of<ILogger<LoginAccountCommandHandler>>();
        var postCommitQueue = CreatePostCommitActionQueue();
        var securityOptions = Options.Create(new SecuritySettings { MaxOtpAttempts = 2 });

        var user = new ApplicationUser
        {
            Id = "identity-id",
            UserName = "employee001",
            NormalizedUserName = "EMPLOYEE001",
            Email = "employee@example.com",
            NormalizedEmail = "EMPLOYEE@EXAMPLE.COM",
            EmailConfirmed = true,
            IsActive = true,
            AccessFailedCount = 1
        };
        user.PasswordHash = userManager.Object.PasswordHasher.HashPassword(user, "CorrectPassword123!");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new LoginAccountCommandHandler(
            userManager.Object,
            signInManager.Object,
            context,
            queue.Object,
            postCommitQueue,
            new KeyedLockProvider(),
            securityOptions,
            logger);

        var result = await handler.Handle(
            new LoginAccountCommand("employee001", "WrongPassword!", false, "127.0.0.1"),
            CancellationToken.None);

        await postCommitQueue.FlushAsync();

        Assert.False(result.IsAuthenticated);
        Assert.Contains("locked", result.Message, StringComparison.OrdinalIgnoreCase);

        var updatedUser = await context.Users.SingleAsync(x => x.Id == "identity-id");
        Assert.NotNull(updatedUser.LockoutEnd);
        Assert.Equal(0, updatedUser.AccessFailedCount);
        Assert.NotNull(updatedUser.LastLockoutAlertSentAt);

        queue.Verify(
            x => x.QueueEmailAsync(
                It.Is<IEmailBackgroundMessage>(message =>
                    message.GetType() == typeof(ItLockoutAlertMessage) &&
                    ((ItLockoutAlertMessage)message).Email == "employee@example.com" &&
                    ((ItLockoutAlertMessage)message).UserId == "employee001"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    /// <summary>
    /// Runs the Argon2idPasswordHasher_ReturnsSuccessRehashNeeded_ForLegacyCustomHash operation.
    /// </summary>
    public void Argon2idPasswordHasher_ReturnsSuccessRehashNeeded_ForLegacyCustomHash()
    {
        var settings = CreateTestSecuritySettings();
        var hasher = new Argon2idPasswordHasher<ApplicationUser>(Options.Create(settings));
        var user = new ApplicationUser { Id = "legacy-user", UserName = "legacy" };
        var legacyHash = CreateLegacyArgon2Hash("CorrectPassword123!", settings.LegacyArgon2id);

        var result = hasher.VerifyHashedPassword(user, legacyHash, "CorrectPassword123!");

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
    }

    [Fact]
    /// <summary>
    /// Runs the LoginHandler_RehashesLegacyIdentityPassword_OnSuccessfulLogin operation.
    /// </summary>
    public async Task LoginHandler_RehashesLegacyIdentityPassword_OnSuccessfulLogin()
    {
        using var context = CreateContext();

        var settings = CreateTestSecuritySettings();
        var argon2Hasher = new Argon2idPasswordHasher<ApplicationUser>(Options.Create(settings));
        var legacyHasher = new PasswordHasher<ApplicationUser>();
        var userManager = CreateUserManager();
        userManager.Object.PasswordHasher = argon2Hasher;
        var signInManager = CreateSignInManager(userManager.Object);
        var queue = new Mock<IEmailBackgroundQueue>();
        var logger = Mock.Of<ILogger<LoginAccountCommandHandler>>();
        var postCommitQueue = CreatePostCommitActionQueue();
        var securityOptions = Options.Create(settings);

        var user = new ApplicationUser
        {
            Id = "identity-id",
            UserName = "employee001",
            NormalizedUserName = "EMPLOYEE001",
            Email = "employee@example.com",
            NormalizedEmail = "EMPLOYEE@EXAMPLE.COM",
            EmailConfirmed = true,
            IsActive = true
        };
        user.PasswordHash = legacyHasher.HashPassword(user, "CorrectPassword123!");
        var legacyStoredHash = user.PasswordHash;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new LoginAccountCommandHandler(
            userManager.Object,
            signInManager.Object,
            context,
            queue.Object,
            postCommitQueue,
            new KeyedLockProvider(),
            securityOptions,
            logger);

        var result = await handler.Handle(
            new LoginAccountCommand("employee001", "CorrectPassword123!", false, "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsAuthenticated);

        context.ChangeTracker.Clear();
        var updatedUser = await context.Users.SingleAsync(x => x.Id == "identity-id");
        Assert.NotEqual(legacyStoredHash, updatedUser.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            argon2Hasher.VerifyHashedPassword(updatedUser, updatedUser.PasswordHash ?? string.Empty, "CorrectPassword123!"));
    }

    [Fact]
    /// <summary>
    /// Runs the ForgotPasswordValidator_Fails_WhenUsernameMissing operation.
    /// </summary>
    public void ForgotPasswordValidator_Fails_WhenUsernameMissing()
    {
        var validator = new ForgotPasswordCommandValidator();

        var result = validator.Validate(new ForgotPasswordCommand(string.Empty, "127.0.0.1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the ForgotPasswordHandler_ReturnsInvalidUser_WhenUserMissing operation.
    /// </summary>
    public async Task ForgotPasswordHandler_ReturnsInvalidUser_WhenUserMissing()
    {
        using var context = CreateContext();
        var queue = new Mock<IEmailBackgroundQueue>();
        var logger = Mock.Of<ILogger<ForgotPasswordCommandHandler>>();
        var keyedLockProvider = new KeyedLockProvider();

        var handler = new ForgotPasswordCommandHandler(
            context,
            new SecureOtpGenerator(),
            queue.Object,
            keyedLockProvider,
            logger);

        var result = await handler.Handle(new ForgotPasswordCommand("missing", "127.0.0.1"), CancellationToken.None);

        Assert.False(result.OtpSent);
        Assert.Equal("Invalid username or email address.", result.Message);
    }

    [Fact]
    /// <summary>
    /// Runs the SecureOtpGenerator_GeneratesSixCharacterHighEntropyCodes operation.
    /// </summary>
    public void SecureOtpGenerator_GeneratesSixCharacterHighEntropyCodes()
    {
        var generator = new SecureOtpGenerator();

        var firstCode = generator.GenerateCode();
        var secondCode = generator.GenerateCode();

        Assert.Matches("^[2-9A-HJ-NP-Z]{6}$", firstCode);
        Assert.Matches("^[2-9A-HJ-NP-Z]{6}$", secondCode);
        Assert.NotEqual(firstCode, secondCode);
    }

    [Fact]
    /// <summary>
    /// Runs the EmailOtpService_ReusesExistingTransaction_WhenOneIsAlreadyOpen operation.
    /// </summary>
    public async Task EmailOtpService_ReusesExistingTransaction_WhenOneIsAlreadyOpen()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var http = new Mock<IHttpContextAccessor>();
        var dbLogger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string s) => s);
        encryption.Setup(x => x.DecryptKey(It.IsAny<string>())).Returns((string s) => s);

        await using var context = new ApplicationDbContext(options, http.Object, dbLogger.Object, encryption.Object);
        await context.Database.EnsureCreatedAsync();

        var service = new EmailOtpService(
            context,
            Mock.Of<ILogger<EmailOtpService>>(),
            Mock.Of<IOutlookService>(),
            DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))),
            Options.Create(new SecuritySettings { OtpExpiryMinutes = 3 }));

        await using var transaction = await context.Database.BeginTransactionAsync();

        var code = await service.GenerateOtpAsync("user@example.com", "127.0.0.1", false);

        Assert.Equal(6, code.Length);
        Assert.Equal(1, await context.OtpRecords.CountAsync());
        Assert.NotNull(context.Database.CurrentTransaction);

        await transaction.CommitAsync();
    }

    [Fact]
    /// <summary>
    /// Runs the VerifyOtpValidator_Fails_WhenCodeMissing operation.
    /// </summary>
    public void VerifyOtpValidator_Fails_WhenCodeMissing()
    {
        var validator = new VerifyOtpCommandValidator();

        var result = validator.Validate(new VerifyOtpCommand("user@example.com", string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the VerifyOtpHandler_ThrowsValidationException_WhenOtpIsMissing operation.
    /// </summary>
    public async Task VerifyOtpHandler_ThrowsValidationException_WhenOtpIsMissing()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync("user@example.com")).ReturnsAsync(new ApplicationUser
        {
            Id = "otp-user",
            Email = "user@example.com"
        });
        var keyedLockProvider = new KeyedLockProvider();
        var handler = new VerifyOtpCommandHandler(
            userManager.Object,
            context,
            keyedLockProvider,
            Mock.Of<ILogger<VerifyOtpCommandHandler>>());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new VerifyOtpCommand("user@example.com", "ABC234"), CancellationToken.None));

        Assert.Contains("\u9a8c\u8bc1\u7801\u5df2\u8fc7\u671f\u6216\u4e0d\u5b58\u5728", exception.Errors.Select(error => error.ErrorMessage));
    }

    [Fact]
    /// <summary>
    /// Runs the VerifyOtpHandler_InvalidatesOtpAfterThreeFailures operation.
    /// </summary>
    public async Task VerifyOtpHandler_InvalidatesOtpAfterThreeFailures()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateSqliteContext(connection);
        await context.Database.EnsureCreatedAsync();
        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync("user@example.com")).ReturnsAsync(new ApplicationUser
        {
            Id = "otp-user",
            Email = "user@example.com"
        });

        var keyedLockProvider = new KeyedLockProvider();
        context.OtpRecords.Add(new OtpRecord
        {
            Email = "user@example.com",
            Code = AccountOtpSupport.HashCode("ABC234"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            FailedAttempts = 2,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new VerifyOtpCommandHandler(
            userManager.Object,
            context,
            keyedLockProvider,
            Mock.Of<ILogger<VerifyOtpCommandHandler>>());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new VerifyOtpCommand("user@example.com", "ZZZ999"), CancellationToken.None));

        Assert.Contains("\u8fde\u7eed\u8f93\u9519 3 \u6b21\uff0c\u9a8c\u8bc1\u7801\u5df2\u4f5c\u5e9f\uff0c\u8bf7\u91cd\u65b0\u83b7\u53d6", exception.Errors.Select(error => error.ErrorMessage));
        context.ChangeTracker.Clear();
        var storedOtp = await context.OtpRecords.SingleAsync();
        Assert.True(storedOtp.IsLocked);
        Assert.True(storedOtp.IsUsed);
    }

    [Fact]
    /// <summary>
    /// Runs the VerifyOtpHandler_ConsumesOtpOnlyOnce_WhenRequestsRace operation.
    /// </summary>
    public async Task VerifyOtpHandler_ConsumesOtpOnlyOnce_WhenRequestsRace()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var seedContext = CreateSqliteContext(connection);
        await seedContext.Database.EnsureCreatedAsync();
        seedContext.OtpRecords.Add(new OtpRecord
        {
            Email = "user@example.com",
            Code = AccountOtpSupport.HashCode("ABC234"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });
        await seedContext.SaveChangesAsync();

        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync("user@example.com")).ReturnsAsync(new ApplicationUser
        {
            Id = "otp-user",
            Email = "user@example.com"
        });
        userManager.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
            .Returns(async () =>
            {
                await Task.Delay(50);
                return Guid.NewGuid().ToString("N");
            });

        await using var context1 = CreateSqliteContext(connection);
        await using var context2 = CreateSqliteContext(connection);
        var keyedLockProvider = new KeyedLockProvider();

        var handler1 = new VerifyOtpCommandHandler(
            userManager.Object,
            context1,
            keyedLockProvider,
            Mock.Of<ILogger<VerifyOtpCommandHandler>>());
        var handler2 = new VerifyOtpCommandHandler(
            userManager.Object,
            context2,
            keyedLockProvider,
            Mock.Of<ILogger<VerifyOtpCommandHandler>>());

        var tasks = new[]
        {
            handler1.Handle(new VerifyOtpCommand("user@example.com", "ABC234"), CancellationToken.None),
            handler2.Handle(new VerifyOtpCommand("user@example.com", "ABC234"), CancellationToken.None)
        };

        var outcomes = await Task.WhenAll(tasks.Select(async task =>
        {
            try
            {
                var result = await task;
                return (Succeeded: true, Result: result, Error: (ValidationException?)null);
            }
            catch (ValidationException ex)
            {
                return (Succeeded: false, Result: (VerifyOtpResult?)null, Error: ex);
            }
        }));

        Assert.Single(outcomes, x => x.Succeeded);
        Assert.Single(outcomes, x => !x.Succeeded);

        await using var verificationContext = CreateSqliteContext(connection);
        var storedOtp = await verificationContext.OtpRecords.SingleAsync();
        Assert.True(storedOtp.IsUsed);
    }

    [Fact]
    /// <summary>
    /// Runs the ResetPasswordValidator_Fails_WhenFieldsMissing operation.
    /// </summary>
    public void ResetPasswordValidator_Fails_WhenFieldsMissing()
    {
        var validator = new ResetPasswordCommandValidator();

        var result = validator.Validate(new ResetPasswordCommand(string.Empty, string.Empty, string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the ResetPasswordHandler_ReturnsUserNotFound_WhenEmailUnknown operation.
    /// </summary>
    public async Task ResetPasswordHandler_ReturnsUserNotFound_WhenEmailUnknown()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager();
        userManager.Setup(x => x.FindByEmailAsync("missing@example.com")).ReturnsAsync((ApplicationUser?)null);

        var queue = new Mock<IEmailBackgroundQueue>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Mock.Of<ILogger<ResetPasswordCommandHandler>>();

        var handler = new ResetPasswordCommandHandler(userManager.Object, context, queue.Object, cache, logger);

        var result = await handler.Handle(
            new ResetPasswordCommand("missing@example.com", "token", "Password123!"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(result.ShouldRedirectToLogin);
        Assert.Empty(result.Errors);
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetProfilePageQueryHandler_ReturnsTypedProfile operation.
    /// </summary>
    public async Task GetProfilePageQueryHandler_ReturnsTypedProfile()
    {
        using var context = CreateContext();
        context.Users.Add(new ApplicationUser
        {
            Id = "u-profile",
            FullName = "Profile User",
            Email = "profile@test.local",
            JobTitle = "IT Executive",
            Signature = "data:image/png;base64,abc",
            EmailSignatureImage = "data:image/png;base64,def",
            UserTitles = [new UserTitle { Title = "IT" }, new UserTitle { Title = "HR" }]
        });
        await context.SaveChangesAsync();

        var handler = new GetProfilePageQueryHandler(context);

        var result = await handler.Handle(new GetProfilePageQuery("u-profile"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Profile User", result.Data!.FullName);
        Assert.Equal(["IT", "HR"], result.Data.Titles);
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetLoginPageBrandingQueryHandler_ReturnsConfiguredBrandingAndSelectedLogo operation.
    /// </summary>
    public async Task GetLoginPageBrandingQueryHandler_ReturnsConfiguredBrandingAndSelectedLogo()
    {
        using var context = CreateContext();
        context.Companies.AddRange(
            new Company
            {
                Name = "Inactive Logo",
                IsActive = false,
                IsLoginPageLogo = true,
                LogoPath = "/logos/inactive.png"
            },
            new Company
            {
                Name = "Active Logo",
                IsActive = true,
                IsLoginPageLogo = true,
                LogoPath = "/logos/active.png"
            });
        await context.SaveChangesAsync();

        var options = Options.Create(new LoginPageBrandingOptions
        {
            CompanyName = "Contoso",
            SystemName = "Portal",
            Subtitle = "Secure Access",
            BackgroundImagePath = "/images/login-bg.jpg",
            SystemVersion = "2026.04.08"
        });

        var handler = new GetLoginPageBrandingQueryHandler(context, options);

        var result = await handler.Handle(
            new GetLoginPageBrandingQuery(
                new IT_Form.ViewModels.LoginViewModel
                {
                    Username = "alice",
                    RememberMe = true
                },
                "2026-04-08T12:34:56Z"),
            CancellationToken.None);

        Assert.Equal("alice", result.Username);
        Assert.True(result.RememberMe);
        Assert.Equal("Contoso", result.CompanyName);
        Assert.Equal("Portal", result.SystemName);
        Assert.Equal("Secure Access", result.Subtitle);
        Assert.Equal("/images/login-bg.jpg", result.BackgroundImagePath);
        Assert.Equal("/logos/active.png", result.LoginLogoPath);
        Assert.Equal("2026.04.08", result.SystemVersion);
        Assert.Equal("2026-04-08T12:34:56Z", result.LockoutEndTimeIso);
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetVerifyOtpPageQueryHandler_ReturnsRemainingCooldown operation.
    /// </summary>
    public async Task GetVerifyOtpPageQueryHandler_ReturnsRemainingCooldown()
    {
        using var context = CreateContext();
        context.Users.Add(new ApplicationUser
        {
            Id = "otp-page-user",
            Email = "user@example.com",
            UserName = "user@example.com"
        });
        context.OtpRecords.Add(new OtpRecord
        {
            Email = "user@example.com",
            Code = AccountOtpSupport.HashCode("ABC234"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow.AddSeconds(-10)
        });
        await context.SaveChangesAsync();

        var handler = new GetVerifyOtpPageQueryHandler(context);

        var result = await handler.Handle(
            new GetVerifyOtpPageQuery("user@example.com", "ABC234"),
            CancellationToken.None);

        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("ABC234", result.Code);
        Assert.InRange(result.RemainingCooldownSeconds, 45, 55);
    }

    [Fact]
    /// <summary>
    /// Runs the ResendOtpCommandHandler_QueuesOtpEmail_WhenEligible operation.
    /// </summary>
    public async Task ResendOtpCommandHandler_QueuesOtpEmail_WhenEligible()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateSqliteContext(connection);
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new ApplicationUser
        {
            Id = "u-otp",
            Email = "user@example.com",
            UserName = "user@example.com",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var queue = new Mock<IEmailBackgroundQueue>();
        OtpEmailMessage? queuedMessage = null;
        queue.Setup(x => x.QueueEmailAsync(It.IsAny<IEmailBackgroundMessage>(), It.IsAny<CancellationToken>()))
            .Callback<IEmailBackgroundMessage, CancellationToken>((message, _) => queuedMessage = Assert.IsType<OtpEmailMessage>(message))
            .Returns(ValueTask.CompletedTask);
        var logger = Mock.Of<ILogger<ResendOtpCommandHandler>>();
        var keyedLockProvider = new KeyedLockProvider();

        var handler = new ResendOtpCommandHandler(
            context,
            new SecureOtpGenerator(),
            queue.Object,
            keyedLockProvider,
            logger);

        var result = await handler.Handle(
            new ResendOtpCommand("user@example.com", "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsQueued);
        Assert.Equal("user@example.com", result.Email);
        var storedOtp = await context.OtpRecords.SingleAsync();
        Assert.StartsWith("sha256:", storedOtp.Code);
        Assert.NotNull(queuedMessage);
        var codeMatch = Regex.Match(queuedMessage!.HtmlBody, "[2-9A-HJ-NP-Z]{6}");
        Assert.True(codeMatch.Success);
        Assert.Equal(AccountOtpSupport.HashCode(codeMatch.Value), storedOtp.Code);
        queue.Verify(
            x => x.QueueEmailAsync(
                It.Is<IEmailBackgroundMessage>(message =>
                    message.GetType() == typeof(OtpEmailMessage) &&
                    ((OtpEmailMessage)message).Email == "user@example.com" &&
                    ((OtpEmailMessage)message).Subject.Contains("Password Recovery Code") &&
                    ((OtpEmailMessage)message).HtmlBody.Contains(codeMatch.Value)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    /// <summary>
    /// Runs the LogoutCommandHandler_SignsOutAndWritesAuditLog operation.
    /// </summary>
    public async Task LogoutCommandHandler_SignsOutAndWritesAuditLog()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "u-logout"),
            new Claim(ClaimTypes.Name, "logout.user")
        ], "TestAuth")));

        var logger = Mock.Of<ILogger<LogoutCommandHandler>>();
        var handler = new LogoutCommandHandler(signInManager.Object, currentUser.Object, context, logger);

        await handler.Handle(new LogoutCommand(), CancellationToken.None);

        signInManager.Verify(x => x.SignOutAsync(), Times.Once);
        var auditLog = context.AuditLogs.Single();
        Assert.Equal("Logout", auditLog.Action);
        Assert.Equal("logout.user", auditLog.PerformedBy);
        Assert.Equal("u-logout", auditLog.EntityId);
    }

    [Fact]
    /// <summary>
    /// Runs the ForceChangePasswordValidator_Fails_WhenNewPasswordMatchesCurrent operation.
    /// </summary>
    public void ForceChangePasswordValidator_Fails_WhenNewPasswordMatchesCurrent()
    {
        var validator = new ForceChangePasswordCommandValidator(
            Options.Create(new IdentityOptions
            {
                Password =
                {
                    RequiredLength = 10,
                    RequireDigit = true,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireNonAlphanumeric = true,
                    RequiredUniqueChars = 4
                }
            }));

        var result = validator.Validate(new ForceChangePasswordCommand(
            "u1",
            "Password123!",
            "Password123!",
            "Password123!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "New password must be different from the current password.");
    }

    [Fact]
    public void BeginProfilePasswordChangeValidator_RequiresDoubleVerificationInputs()
    {
        var validator = new BeginProfilePasswordChangeCommandValidator(
            Options.Create(new IdentityOptions
            {
                Password =
                {
                    RequiredLength = 10,
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireUppercase = true,
                    RequireNonAlphanumeric = true,
                    RequiredUniqueChars = 4
                }
            }));

        var result = validator.Validate(new BeginProfilePasswordChangeCommand(
            "user-id",
            "CurrentPassword123!",
            "CurrentPassword123!",
            "DifferentPassword123!",
            "127.0.0.1"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "New password must be different from the current password.");
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Passwords do not match.");
    }

    [Fact]
    public async Task BeginProfilePasswordChangeHandler_VerifiesCurrentPasswordAndQueuesOtp()
    {
        using var context = CreateContext();
        var user = new ApplicationUser
        {
            Id = "profile-change-user",
            UserName = "profile.user",
            Email = "profile.user@example.com",
            FullName = "Profile User",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(manager => manager.CheckPasswordAsync(user, "CurrentPassword123!")).ReturnsAsync(true);
        var queue = new Mock<IEmailBackgroundQueue>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var dataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
        var handler = new BeginProfilePasswordChangeCommandHandler(
            userManager.Object,
            context,
            new SecureOtpGenerator(),
            queue.Object,
            new KeyedLockProvider(),
            cache,
            dataProtectionProvider,
            Mock.Of<ILogger<BeginProfilePasswordChangeCommandHandler>>());

        var result = await handler.Handle(new BeginProfilePasswordChangeCommand(
            user.Id,
            "CurrentPassword123!",
            "NewPassword123!",
            "NewPassword123!",
            "127.0.0.1"), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.RequestId));
        Assert.Equal(user.Email, result.Email);
        Assert.Single(context.OtpRecords.Where(otp => otp.Email == user.Email && otp.Purpose == OtpPurposes.ProfilePasswordChange));
        Assert.True(cache.TryGetValue($"profile-password-change:{user.Id}:{result.RequestId}", out string? protectedPayload));
        Assert.False(string.IsNullOrWhiteSpace(protectedPayload));
        queue.Verify(queue => queue.QueueEmailAsync(
            It.Is<OtpEmailMessage>(message =>
                message.Email == user.Email &&
                message.Subject.Contains("Confirm Password Change")),
            It.IsAny<CancellationToken>()), Times.Once);
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

    /// <summary>
    /// Creates data for the CreateSignInManager operation.
    /// </summary>
    private static Mock<SignInManager<ApplicationUser>> CreateSignInManager(UserManager<ApplicationUser> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(x => x.Value).Returns(new IdentityOptions());

        var logger = new Mock<ILogger<SignInManager<ApplicationUser>>>();
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        var confirmation = new Mock<IUserConfirmation<ApplicationUser>>();

        return new Mock<SignInManager<ApplicationUser>>(
            userManager,
            contextAccessor.Object,
            claimsFactory.Object,
            options.Object,
            logger.Object,
            schemes.Object,
            confirmation.Object);
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
    /// Creates data for the CreateSqliteContext operation.
    /// </summary>
    private static ApplicationDbContext CreateSqliteContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var http = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string s) => s);
        encryption.Setup(x => x.DecryptKey(It.IsAny<string>())).Returns((string s) => s);

        return new ApplicationDbContext(options, http.Object, logger.Object, encryption.Object);
    }

    /// <summary>
    /// Creates data for the CreatePostCommitActionQueue operation.
    /// </summary>
    private static PostCommitActionQueue CreatePostCommitActionQueue()
        => new(Mock.Of<ILogger<PostCommitActionQueue>>());

    /// <summary>
    /// Creates data for the CreateTestSecuritySettings operation.
    /// </summary>
    private static SecuritySettings CreateTestSecuritySettings()
        => new()
        {
            MaxOtpAttempts = 3,
            Argon2id = new Argon2idSettings
            {
                DegreeOfParallelism = 2,
                MemorySize = 1024,
                Iterations = 1,
                SaltSize = 16,
                HashSize = 32
            },
            LegacyArgon2id = new Argon2idSettings
            {
                DegreeOfParallelism = 2,
                MemorySize = 1024,
                Iterations = 2,
                SaltSize = 16,
                HashSize = 32
            }
        };

    /// <summary>
    /// Creates data for the CreateLegacyArgon2Hash operation.
    /// </summary>
    private static string CreateLegacyArgon2Hash(string password, Argon2idSettings settings)
    {
        var salt = Enumerable.Range(1, settings.SaltSize).Select(value => (byte)value).ToArray();
        using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = settings.DegreeOfParallelism,
            MemorySize = settings.MemorySize,
            Iterations = settings.Iterations
        };

        var hash = argon2.GetBytes(settings.HashSize);
        var result = new byte[1 + settings.SaltSize + settings.HashSize];
        result[0] = 0x01;
        Buffer.BlockCopy(salt, 0, result, 1, settings.SaltSize);
        Buffer.BlockCopy(hash, 0, result, 1 + settings.SaltSize, settings.HashSize);
        return Convert.ToBase64String(result);
    }
}
