// Project comment: VerificationControllerTests.cs is part of the IT Form application source code.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the VerificationControllerTests class used by the IT Form application.
    /// </summary>
    public class VerificationControllerTests : ControllerTestBase
    {
        // VerificationController uses Index(token)

        [Fact]
        /// <summary>
        /// Runs the Index_ReturnsView_ForValidToken operation.
        /// </summary>
        public async Task Index_ReturnsView_ForValidToken()
        {
            using var context = CreateContext();
            var empId = Guid.NewGuid().ToString();
            context.Users.Add(new ApplicationUser { Id = empId, UserName = "test", Email = "test@ytc.com" });
            
            var token = new DeviceVerificationToken { Token = Guid.NewGuid().ToString(), EmployeeId = empId, IsVerified = false };
            context.DeviceVerificationTokens.Add(token);
            await context.SaveChangesAsync();

            var controller = GetController<VerificationController>(context);
            var result = await controller.Index(token.Token);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the Index_ReturnsBadRequest_ForExpiredToken operation.
        /// </summary>
        public async Task Index_ReturnsBadRequest_ForExpiredToken()
        {
            using var context = CreateContext();
            var empId = Guid.NewGuid().ToString();
            context.Users.Add(new ApplicationUser { Id = empId, UserName = "expired", Email = "expired@ytc.com" });
            context.DeviceVerificationTokens.Add(new DeviceVerificationToken
            {
                Token = Guid.NewGuid().ToString(),
                EmployeeId = empId,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            });
            await context.SaveChangesAsync();

            var controller = CreateVerificationController(
                context,
                new Dictionary<string, string?>
                {
                    ["SecuritySettings:DeviceVerificationExpiryMinutes"] = "30"
                },
                "localhost",
                5063);

            var token = await context.DeviceVerificationTokens.FirstAsync();
            var result = await controller.Index(token.Token);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the DownloadUniversalTool_ReturnsNotFound_AfterSecurityHardening operation.
        /// </summary>
        public void DownloadUniversalTool_ReturnsNotFound_AfterSecurityHardening()
        {
            using var context = CreateContext();
            var controller = CreateVerificationController(
                context,
                new Dictionary<string, string?>(),
                "testhost.local",
                6000);

            var result = controller.DownloadUniversalTool();
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the GenerateToken_ReturnsEmployeeSpecificLink_AndReplacesOlderTokens operation.
        /// </summary>
        public async Task GenerateToken_ReturnsEmployeeSpecificLink_AndReplacesOlderTokens()
        {
            using var context = CreateContext();
            var empId = Guid.NewGuid().ToString();
            context.Users.Add(new ApplicationUser
            {
                Id = empId,
                UserName = "verified-user",
                Email = "verified-user@ytc.com",
                FullName = "Verified User",
                UserCompanies = new List<UserCompany>
                {
                    new UserCompany { CompanyName = "HQ" }
                }
            });
            context.DeviceVerificationTokens.Add(new DeviceVerificationToken
            {
                Token = Guid.NewGuid().ToString("N"),
                EmployeeId = empId,
                IsVerified = false
            });
            await context.SaveChangesAsync();

            var controller = CreateVerificationController(
                context,
                new Dictionary<string, string?>
                {
                    ["SecuritySettings:DeviceVerificationExpiryMinutes"] = "30"
                },
                "localhost",
                5063);

            var result = await controller.GenerateToken(empId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            var link = ok.Value.GetType().GetProperty("link")?.GetValue(ok.Value) as string;
            Assert.False(string.IsNullOrWhiteSpace(link));
            Assert.Contains("/verify/", link, StringComparison.OrdinalIgnoreCase);

            var tokens = await context.DeviceVerificationTokens
                .Where(t => t.EmployeeId == empId)
                .ToListAsync();

            var current = Assert.Single(tokens);
            Assert.False(current.IsVerified);
            Assert.Contains(current.Token, link, StringComparison.OrdinalIgnoreCase);
        }


        [Fact]
        public async Task GenerateToken_RejectsEmployeeOutsideCurrentCompanyScope()
        {
            using var context = CreateContext();
            var employeeId = Guid.NewGuid().ToString();
            context.Users.Add(new ApplicationUser
            {
                Id = employeeId,
                UserName = "other-company",
                Email = "other-company@example.com",
                FullName = "Other Company User",
                UserCompanies = new List<UserCompany>
                {
                    new UserCompany { CompanyName = "Company B" }
                }
            });
            await context.SaveChangesAsync();

            var controller = CreateVerificationController(
                context,
                new Dictionary<string, string?>(),
                "localhost",
                5063);

            var result = await controller.GenerateToken(employeeId);

            Assert.IsType<ForbidResult>(result);
            Assert.Empty(await context.DeviceVerificationTokens.ToListAsync());
        }

        [Fact]
        /// <summary>
        /// Runs the GenerateTokenWithoutEmployee_ReturnsAssetOnlyLink_AndStoresAnonymousToken operation.
        /// </summary>
        public async Task GenerateTokenWithoutEmployee_ReturnsAssetOnlyLink_AndStoresAnonymousToken()
        {
            using var context = CreateContext();

            var controller = CreateVerificationController(
                context,
                new Dictionary<string, string?>
                {
                    ["SecuritySettings:DeviceVerificationExpiryMinutes"] = "30"
                },
                "localhost",
                5063);

            var result = await controller.GenerateTokenWithoutEmployee();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            var link = ok.Value.GetType().GetProperty("link")?.GetValue(ok.Value) as string;
            var assignmentMode = ok.Value.GetType().GetProperty("assignmentMode")?.GetValue(ok.Value) as string;
            var expiresAtUtc = ok.Value.GetType().GetProperty("expiresAtUtc")?.GetValue(ok.Value);

            Assert.False(string.IsNullOrWhiteSpace(link));
            Assert.Equal("asset-only", assignmentMode);
            Assert.NotNull(expiresAtUtc);
            Assert.Contains("/verify/", link, StringComparison.OrdinalIgnoreCase);

            var storedToken = await context.DeviceVerificationTokens.SingleAsync();
            Assert.Null(storedToken.EmployeeId);
            Assert.False(storedToken.IsVerified);
            Assert.Contains(storedToken.Token, link, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        /// <summary>
        /// Runs the Index_RejectsExpiredAssetOnlyToken operation.
        /// </summary>
        public async Task Index_RejectsExpiredAssetOnlyToken()
        {
            using var context = CreateContext();
            context.DeviceVerificationTokens.Add(new DeviceVerificationToken
            {
                Token = Guid.NewGuid().ToString("N"),
                EmployeeId = null,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                IsVerified = false
            });
            await context.SaveChangesAsync();

            var controller = CreateVerificationController(
                context,
                new Dictionary<string, string?>
                {
                    ["SecuritySettings:DeviceVerificationExpiryMinutes"] = "30"
                },
                "localhost",
                5063);

            var token = await context.DeviceVerificationTokens.FirstAsync();
            var result = await controller.Index(token.Token);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the DownloadTool_RejectsExpiredAssetOnlyToken operation.
        /// </summary>
        public async Task DownloadTool_RejectsExpiredAssetOnlyToken()
        {
            using var context = CreateContext();
            context.DeviceVerificationTokens.Add(new DeviceVerificationToken
            {
                Token = Guid.NewGuid().ToString("N"),
                EmployeeId = null,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                IsVerified = false
            });
            await context.SaveChangesAsync();

            var controller = CreateVerificationController(
                context,
                new Dictionary<string, string?>
                {
                    ["SecuritySettings:DeviceVerificationExpiryMinutes"] = "30"
                },
                "localhost",
                5063);

            var token = await context.DeviceVerificationTokens.FirstAsync();
            var result = await controller.DownloadTool(token.Token);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the DownloadTool_RejectsExpiredToken operation.
        /// </summary>
        public async Task DownloadTool_RejectsExpiredToken()
        {
            using var context = CreateContext();
            var empId = Guid.NewGuid().ToString();
            context.Users.Add(new ApplicationUser { Id = empId, UserName = "expired-dl", Email = "expired-dl@ytc.com" });
            context.DeviceVerificationTokens.Add(new DeviceVerificationToken
            {
                Token = Guid.NewGuid().ToString("N"),
                EmployeeId = empId,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            });
            await context.SaveChangesAsync();

            var controller = CreateVerificationController(
                context,
                new Dictionary<string, string?>
                {
                    ["SecuritySettings:DeviceVerificationExpiryMinutes"] = "30"
                },
                "localhost",
                5063);

            var token = await context.DeviceVerificationTokens.FirstAsync();
            var result = await controller.DownloadTool(token.Token);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        /// <summary>
        /// Gets the data required by the GetStatus_ReturnsFingerprintWarning_WhenVerifiedTokenHasMismatch operation.
        /// </summary>
        public async Task GetStatus_ReturnsFingerprintWarning_WhenVerifiedTokenHasMismatch()
        {
            using var context = CreateContext();
            context.DeviceVerificationTokens.Add(new DeviceVerificationToken
            {
                Token = Guid.NewGuid().ToString("N"),
                IsVerified = true,
                HasHardwareFingerprintMismatch = true,
                HardwareFingerprintWarning = "Hardware changed.",
                VerifiedAssetTag = "LAP-900"
            });
            await context.SaveChangesAsync();

            var controller = CreateVerificationController(
                context,
                new Dictionary<string, string?>(),
                "localhost",
                5063);

            var token = await context.DeviceVerificationTokens.FirstAsync();
            var result = await controller.GetStatus(token.Token);

            var json = Assert.IsType<JsonResult>(result);
            Assert.NotNull(json.Value);

            var valueType = json.Value.GetType();
            Assert.True((bool)(valueType.GetProperty("isVerified")?.GetValue(json.Value) ?? false));
            Assert.True((bool)(valueType.GetProperty("fingerprintMismatch")?.GetValue(json.Value) ?? false));
            Assert.Equal("Hardware changed.", valueType.GetProperty("warningMessage")?.GetValue(json.Value) as string);
            Assert.Equal("LAP-900", valueType.GetProperty("assetTag")?.GetValue(json.Value) as string);
        }

        /// <summary>
        /// Creates data for the CreateVerificationController operation.
        /// </summary>
        private static VerificationController CreateVerificationController(
            Data.ApplicationDbContext context,
            IDictionary<string, string?> configurationValues,
            string host,
            int port)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();

            var controller = new VerificationController(context, configuration);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "it-test-user"),
                    new Claim("UserTitle", "IT"),
                    new Claim("UserCompany", "HQ")
                ],
                "Test"));

            controller.ControllerContext.HttpContext.Request.Scheme = "http";
            controller.ControllerContext.HttpContext.Request.Host = new HostString(host, port);
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(helper => helper.Action(It.IsAny<UrlActionContext>()))
                .Returns<UrlActionContext>(actionContext =>
                    $"http://{host}:{port}/verify/{actionContext.Values?.GetType().GetProperty("token")?.GetValue(actionContext.Values)}");
            controller.Url = urlHelper.Object;
            return controller;
        }
    }
}
