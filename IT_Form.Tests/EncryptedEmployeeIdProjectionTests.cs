// Project comment: EncryptedEmployeeIdProjectionTests.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.EmailGroup;
using IT_Form.Application.Features.Offboarding.Create;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace IT_Form.Tests;

/// <summary>
/// Defines the EncryptedEmployeeIdProjectionTests class used by the IT Form application.
/// </summary>
public sealed class EncryptedEmployeeIdProjectionTests
{
    public EncryptedEmployeeIdProjectionTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Runs the EmailGroupMembersQuery_ReturnsDecryptedEmployeeIdLabels_WithSqliteEncryption operation.
    /// </summary>
    public async Task EmailGroupMembersQuery_ReturnsDecryptedEmployeeIdLabels_WithSqliteEncryption()
    {
        await using var harness = await CreateSqliteHarnessAsync();
        var context = harness.Context;

        var company = new Company { Id = 1, Name = "Alpha", IsActive = true };
        var group = new EmailGroup
        {
            Id = 3,
            GroupName = "IT Team",
            GroupEmail = "it.team@test.local",
            IsActive = true,
            EmailGroupCompanies = [new EmailGroupCompany { Company = company, CompanyId = company.Id }]
        };
        var onboarding = new OnboardingRequest
        {
            Id = 11,
            FullName = "Alice Tan",
            EmployeeID = "E-11",
            Department = "IT",
            Status = RequestStatus.Completed,
            RequestDate = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc)
        };

        context.Companies.Add(company);
        context.EmailGroups.Add(group);
        context.OnboardingRequests.Add(onboarding);
        context.RequestEmailGroups.Add(new RequestEmailGroup
        {
            Id = 21,
            EmailGroup = group,
            EmailGroupId = group.Id,
            OnboardingRequest = onboarding,
            OnboardingRequestId = onboarding.Id,
            GroupName = group.GroupName,
            GroupEmail = group.GroupEmail
        });
        await context.SaveChangesAsync();

        var rawStoredEmployeeId = await ReadStoredEmployeeIdAsync(harness.Connection, onboarding.Id);
        Assert.NotNull(rawStoredEmployeeId);
        Assert.NotEqual("E-11", rawStoredEmployeeId);

        var handler = new GetEmailGroupMembersPageQueryHandler(context, new TestCurrentUserAccessor());
        var result = await handler.Handle(new GetEmailGroupMembersPageQuery(group.Id, onboarding.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<EmailGroupMembersPageViewModel>(result.Data);
        Assert.Equal("Alice Tan (E-11)", model.CurrentEmployeeDisplayName);
        Assert.Contains(model.CurrentEmployees, employee => employee.Id == onboarding.Id && employee.Label == "Alice Tan (E-11)");
    }

    [Fact]
    /// <summary>
    /// Runs the OffboardingEmployeeDetailsQuery_ReturnsDecryptedEmployeeId_WithSqliteEncryption operation.
    /// </summary>
    public async Task OffboardingEmployeeDetailsQuery_ReturnsDecryptedEmployeeId_WithSqliteEncryption()
    {
        await using var harness = await CreateSqliteHarnessAsync();
        var context = harness.Context;

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 42,
            FullName = "Brian Lee",
            EmployeeID = "E-42",
            Department = "Finance",
            Branch = "HQ",
            HODEmail = "hod@test.local",
            EmailAddress = "brian@test.local",
            Status = RequestStatus.Completed,
            RequestDate = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var rawStoredEmployeeId = await ReadStoredEmployeeIdAsync(harness.Connection, 42);
        Assert.NotNull(rawStoredEmployeeId);
        Assert.NotEqual("E-42", rawStoredEmployeeId);

        var handler = new GetOffboardingEmployeeDetailsQueryHandler(context, new TestCurrentUserAccessor());
        var model = await handler.Handle(new GetOffboardingEmployeeDetailsQuery(42), CancellationToken.None);

        Assert.NotNull(model);
        Assert.Equal("Brian Lee", model!.FullName);
        Assert.Equal("E-42", model.EmployeeID);
        Assert.Equal("Finance", model.Department);
    }

    [Fact]
    /// <summary>
    /// Runs the OffboardingEmployeeDetailsQuery_ReturnsNull_ForCrossCompanyAccess operation.
    /// </summary>
    public async Task OffboardingEmployeeDetailsQuery_ReturnsNull_ForCrossCompanyAccess()
    {
        await using var harness = await CreateSqliteHarnessAsync();
        var context = harness.Context;

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 43,
            FullName = "Other Tenant",
            EmployeeID = "E-43",
            Department = "Finance",
            Branch = "HQ",
            HODEmail = "hod@test.local",
            EmailAddress = "other@test.local",
            Status = RequestStatus.Completed,
            RequestDate = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var scopedUser = new TestCurrentUserAccessor(companies: ["OtherCompany"]);
        var handler = new GetOffboardingEmployeeDetailsQueryHandler(context, scopedUser);
        var model = await handler.Handle(new GetOffboardingEmployeeDetailsQuery(43), CancellationToken.None);

        Assert.Null(model);
    }

    /// <summary>
    /// Creates data for the CreateSqliteHarnessAsync operation.
    /// </summary>
    private static async Task<SqliteHarness> CreateSqliteHarnessAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<ApplicationDbContext>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:LicenseEncryptionKey"] = "0123456789ABCDEF0123456789ABCDEF",
                ["Security:LicenseEncryptionIv"] = "0123456789ABCDEF"
            })
            .Build();
        var encryptionService = new LicenseEncryptionService(configuration);

        var context = new ApplicationDbContext(options, httpContextAccessor.Object, logger.Object, encryptionService);
        await context.Database.EnsureCreatedAsync();

        return new SqliteHarness(context, connection);
    }

    /// <summary>
    /// Runs the ReadStoredEmployeeIdAsync operation.
    /// </summary>
    private static async Task<string?> ReadStoredEmployeeIdAsync(SqliteConnection connection, int onboardingRequestId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EmployeeID FROM OnboardingRequests WHERE Id = $id";
        command.Parameters.AddWithValue("$id", onboardingRequestId);

        var value = await command.ExecuteScalarAsync();
        return value as string;
    }

    /// <summary>
    /// Defines the SqliteHarness class used by the IT Form application.
    /// </summary>
    private sealed class SqliteHarness : IAsyncDisposable
    {
        public SqliteHarness(ApplicationDbContext context, SqliteConnection connection)
        {
            Context = context;
            Connection = connection;
        }

        public ApplicationDbContext Context { get; }

        public SqliteConnection Connection { get; }

        /// <summary>
        /// Runs the DisposeAsync operation.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
