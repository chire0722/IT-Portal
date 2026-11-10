// Project comment: StandardSoftwareFeatureSliceTests.cs is part of the IT Form application source code.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using ClosedXML.Excel;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.StandardSoftware.AddSoftwareLicense;
using IT_Form.Application.Features.StandardSoftware.EditSoftwareLicense;
using IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense;
using IT_Form.Application.Features.StandardSoftware;
using IT_Form.Application.Features.StandardSoftware.ImportStandardSoftware;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Features.StandardSoftware.Writes.CreateStandardSoftware;
using IT_Form.Application.Features.StandardSoftware.Writes.DeleteStandardSoftware;
using IT_Form.Application.Features.StandardSoftware.Writes.UpdateStandardSoftware;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the StandardSoftwareFeatureSliceTests class used by the IT Form application.
/// </summary>
public class StandardSoftwareFeatureSliceTests
{
    public StandardSoftwareFeatureSliceTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateStandardSoftwareValidator_Fails_WhenNameMissing operation.
    /// </summary>
    public void CreateStandardSoftwareValidator_Fails_WhenNameMissing()
    {
        var validator = new CreateStandardSoftwareCommandValidator();

        var result = validator.Validate(new CreateStandardSoftwareCommand(string.Empty, false, false, true, false));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the EditStandardSoftwareHandler_ReturnsNotFound_WhenSoftwareMissing operation.
    /// </summary>
    public async Task EditStandardSoftwareHandler_ReturnsNotFound_WhenSoftwareMissing()
    {
        using var context = CreateContext();
        var handler = new UpdateStandardSoftwareCommandHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateStandardSoftwareCommand(999, "Office", false, false, true, false), CancellationToken.None));
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteStandardSoftwareValidator_Fails_WhenAnyAssigned operation.
    /// </summary>
    public async Task DeleteStandardSoftwareValidator_Fails_WhenAnyAssigned()
    {
        using var context = CreateContext();

        var software = new StandardSoftware { Id = 1, SoftwareName = "Office" };
        var license = new SoftwareLicense { Id = 2, StandardSoftwareId = 1, SoftwareName = "Office E3" };
        var key = new SoftwareKey { Id = 3, SoftwareLicenseId = 2, IsAssigned = true, KeyValue = "abc" };
        license.Keys.Add(key);
        software.Licenses.Add(license);

        context.StandardSoftware.Add(software);
        await context.SaveChangesAsync();

        var validator = new DeleteStandardSoftwareCommandValidator(context);
        var result = await validator.ValidateAsync(new DeleteStandardSoftwareCommand(1, "user1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the AddSoftwareLicenseHandler_RequiresExpiry_ForPaidSubscription operation.
    /// </summary>
    public async Task AddSoftwareLicenseHandler_RequiresExpiry_ForPaidSubscription()
    {
        using var context = CreateContext();
        context.StandardSoftware.Add(new StandardSoftware { Id = 10, SoftwareName = "Office" });
        await context.SaveChangesAsync();

        var handler = new AddSoftwareLicenseCommandHandler(
            context,
            new TestCurrentUserAccessor("super-user", titles: ["SuperAdmin"]));
        var result = await handler.Handle(new AddSoftwareLicenseCommand(10, "Office E3", null, true, false, null, 10, null, null, []), CancellationToken.None);

        Assert.Equal(AddSoftwareLicenseStatus.InvalidSubscriptionExpiry, result.Status);
    }


    [Fact]
    public async Task AddSoftwareLicenseHandler_RejectsExcessiveSubscriptionSeatLimit()
    {
        using var context = CreateContext();
        context.StandardSoftware.Add(new StandardSoftware { Id = 13, SoftwareName = "Enterprise App" });
        await context.SaveChangesAsync();

        var quota = new Mock<IQuotaControl>();
        quota.Setup(service => service.EnsureAvailableAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<ClaimsPrincipal?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new AddSoftwareLicenseCommandHandler(
            context,
            new TestCurrentUserAccessor("super-user", titles: ["SuperAdmin"]),
            quota.Object);

        var result = await handler.Handle(
            new AddSoftwareLicenseCommand(
                13,
                "Enterprise App Subscription",
                null,
                true,
                false,
                DateTime.UtcNow.AddDays(30),
                5001,
                null,
                null,
                []),
            CancellationToken.None);

        Assert.Equal(AddSoftwareLicenseStatus.InvalidSeatLimit, result.Status);
        Assert.Empty(await context.SoftwareLicenses.ToListAsync());
    }

    [Fact]
    /// <summary>
    /// Runs the AddSoftwareLicenseHandler_CreatesSeatRows_ForSubscriptionQuantity operation.
    /// </summary>
    public async Task AddSoftwareLicenseHandler_CreatesSeatRows_ForSubscriptionQuantity()
    {
        using var context = CreateContext();
        context.StandardSoftware.Add(new StandardSoftware { Id = 11, SoftwareName = "Microsoft 365" });
        await context.SaveChangesAsync();

        var handler = new AddSoftwareLicenseCommandHandler(
            context,
            new TestCurrentUserAccessor("super-user", titles: ["SuperAdmin"]));
        var result = await handler.Handle(
            new AddSoftwareLicenseCommand(
                11,
                "Microsoft 365 Business",
                null,
                true,
                false,
                DateTime.UtcNow.AddDays(30),
                3,
                null,
                null,
                []),
            CancellationToken.None);

        Assert.Equal(AddSoftwareLicenseStatus.Added, result.Status);

        var license = await context.SoftwareLicenses.Include(item => item.Keys).SingleAsync();
        Assert.Equal(3, license.ContractLimit);
        Assert.Equal(3, license.Keys.Count);
        Assert.All(license.Keys, key => Assert.False(key.IsAssigned));
    }

    [Fact]
    /// <summary>
    /// Runs the AddSoftwareLicenseHandler_AllowsSuperAdmin_ToCreateGlobalVersion operation.
    /// </summary>
    public async Task AddSoftwareLicenseHandler_AllowsSuperAdmin_ToCreateGlobalVersion()
    {
        using var context = CreateContext();
        context.StandardSoftware.Add(new StandardSoftware { Id = 12, SoftwareName = "Adobe Acrobat" });
        await context.SaveChangesAsync();

        var handler = new AddSoftwareLicenseCommandHandler(
            context,
            new TestCurrentUserAccessor("super-user", titles: ["SuperAdmin"]));
        var result = await handler.Handle(
            new AddSoftwareLicenseCommand(
                12,
                "Adobe Acrobat Pro",
                null,
                false,
                false,
                null,
                5,
                null,
                null,
                []),
            CancellationToken.None);

        Assert.Equal(AddSoftwareLicenseStatus.Added, result.Status);
        var license = await context.SoftwareLicenses.Include(item => item.SoftwareLicenseCompanies).SingleAsync();
        Assert.Empty(license.SoftwareLicenseCompanies);
    }

    [Fact]
    /// <summary>
    /// Runs the EditSoftwareLicenseHandler_ReturnsInvalidSeatLimit_WhenReducingBelowAssignedSeats operation.
    /// </summary>
    public async Task EditSoftwareLicenseHandler_ReturnsInvalidSeatLimit_WhenReducingBelowAssignedSeats()
    {
        using var context = CreateContext();
        var license = new SoftwareLicense
        {
            Id = 30,
            StandardSoftwareId = 1,
            SoftwareName = "Office E3",
            IsSubscription = true,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            ContractLimit = 2
        };
        license.Keys.Add(new SoftwareKey { Id = 31, SoftwareLicenseId = 30, KeyValue = "Seat-a1", IsAssigned = true });
        license.Keys.Add(new SoftwareKey { Id = 32, SoftwareLicenseId = 30, KeyValue = "Seat-a2", IsAssigned = false });
        context.SoftwareLicenses.Add(license);
        await context.SaveChangesAsync();

        var handler = new EditSoftwareLicenseCommandHandler(
            context,
            new TestCurrentUserAccessor("super-user", titles: ["SuperAdmin"]));
        var result = await handler.Handle(
            new EditSoftwareLicenseCommand(
                30,
                "Office E3",
                null,
                true,
                false,
                DateTime.UtcNow.AddDays(60),
                0,
                null,
                null,
                []),
            CancellationToken.None);

        Assert.Equal(EditSoftwareLicenseStatus.InvalidSeatLimit, result.Status);
        Assert.Contains("assigned seat", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    /// <summary>
    /// Runs the EditSoftwareLicenseHandler_AllowsSuperAdmin_ToEditScopedVersion operation.
    /// </summary>
    public async Task EditSoftwareLicenseHandler_AllowsSuperAdmin_ToEditScopedVersion()
    {
        using var context = CreateContext();
        context.Companies.Add(new Company { Id = 3, Name = "Alpha", IsActive = true });
        var license = new SoftwareLicense
        {
            Id = 33,
            StandardSoftwareId = 1,
            SoftwareName = "Office E3",
            ContractLimit = 2,
            SoftwareLicenseCompanies =
            [
                new SoftwareLicenseCompany { CompanyId = 3 }
            ]
        };
        context.SoftwareLicenses.Add(license);
        await context.SaveChangesAsync();

        var handler = new EditSoftwareLicenseCommandHandler(
            context,
            new TestCurrentUserAccessor("super-user", titles: ["SuperAdmin"]));
        var result = await handler.Handle(
            new EditSoftwareLicenseCommand(
                33,
                "Office E5",
                null,
                false,
                false,
                null,
                4,
                null,
                null,
                []),
            CancellationToken.None);

        Assert.Equal(EditSoftwareLicenseStatus.Updated, result.Status);
        var saved = await context.SoftwareLicenses.Include(item => item.SoftwareLicenseCompanies).SingleAsync(item => item.Id == 33);
        Assert.Equal("Office E5", saved.SoftwareName);
        Assert.Empty(saved.SoftwareLicenseCompanies);
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteSoftwareLicenseHandler_ReturnsHasAssignedKeys_WhenAssigned operation.
    /// </summary>
    public async Task DeleteSoftwareLicenseHandler_ReturnsHasAssignedKeys_WhenAssigned()
    {
        using var context = CreateContext();
        var license = new SoftwareLicense { Id = 20, StandardSoftwareId = 1, SoftwareName = "Office E3" };
        license.Keys.Add(new SoftwareKey { Id = 21, SoftwareLicenseId = 20, IsAssigned = true, KeyValue = "abc" });
        context.SoftwareLicenses.Add(license);
        await context.SaveChangesAsync();

        var handler = new DeleteSoftwareLicenseCommandHandler(context, new TestCurrentUserAccessor(userName: "it.user", titles: ["IT"], companies: []));
        var result = await handler.Handle(new DeleteSoftwareLicenseCommand(20), CancellationToken.None);

        Assert.Equal(DeleteSoftwareLicenseStatus.HasAssignedKeys, result.Status);
    }

    [Fact]
    /// <summary>
    /// Runs the StandardSoftwareIndexQuery_RestrictsLicensesToAssignedCompany operation.
    /// </summary>
    public async Task StandardSoftwareIndexQuery_RestrictsLicensesToAssignedCompany()
    {
        using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });
        context.StandardSoftware.Add(new StandardSoftware
        {
            Id = 100,
            SoftwareName = "Microsoft 365",
            IsActive = true,
            Licenses =
            [
                new SoftwareLicense
                {
                    Id = 101,
                    SoftwareName = "Alpha License",
                    StandardSoftwareId = 100,
                    SoftwareLicenseCompanies =
                    [
                        new SoftwareLicenseCompany { CompanyId = 1 }
                    ]
                },
                new SoftwareLicense
                {
                    Id = 102,
                    SoftwareName = "Beta License",
                    StandardSoftwareId = 100,
                    SoftwareLicenseCompanies =
                    [
                        new SoftwareLicenseCompany { CompanyId = 2 }
                    ]
                },
                new SoftwareLicense
                {
                    Id = 103,
                    SoftwareName = "Legacy Global License",
                    StandardSoftwareId = 100
                }
            ]
        });
        await context.SaveChangesAsync();

        var handler = new GetStandardSoftwareIndexPageQueryHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["Alpha"]));

        var result = await handler.Handle(
            new GetStandardSoftwareIndexPageQuery(null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var software = Assert.Single(result.Data!.SoftwareList);
        Assert.Equal([101], software.Licenses.Select(license => license.Id).OrderBy(id => id).ToArray());
        Assert.Single(result.Data.CompanyOptions);
    }

    [Fact]
    /// <summary>
    /// Runs the StandardSoftwareIndexQuery_FiltersBySelectedCompany_WhileKeepingGlobalVersions operation.
    /// </summary>
    public async Task StandardSoftwareIndexQuery_FiltersBySelectedCompany_WhileKeepingGlobalVersions()
    {
        using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });
        context.StandardSoftware.Add(new StandardSoftware
        {
            Id = 110,
            SoftwareName = "Adobe Acrobat",
            IsActive = true,
            Licenses =
            [
                new SoftwareLicense
                {
                    Id = 111,
                    SoftwareName = "Global License",
                    StandardSoftwareId = 110
                },
                new SoftwareLicense
                {
                    Id = 112,
                    SoftwareName = "Alpha License",
                    StandardSoftwareId = 110,
                    SoftwareLicenseCompanies =
                    [
                        new SoftwareLicenseCompany { CompanyId = 1 }
                    ]
                },
                new SoftwareLicense
                {
                    Id = 113,
                    SoftwareName = "Beta License",
                    StandardSoftwareId = 110,
                    SoftwareLicenseCompanies =
                    [
                        new SoftwareLicenseCompany { CompanyId = 2 }
                    ]
                }
            ]
        });
        await context.SaveChangesAsync();

        var handler = new GetStandardSoftwareIndexPageQueryHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"]));

        var result = await handler.Handle(
            new GetStandardSoftwareIndexPageQuery(null, null, null, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var software = Assert.Single(result.Data!.SoftwareList);
        Assert.Equal([111, 112], software.Licenses.Select(license => license.Id).OrderBy(id => id).ToArray());
        Assert.Equal(1, result.Data.SelectedCompanyId);
    }


    [Fact]
    public async Task ImportStandardSoftwareHandler_AssociatesNewLicenseWithScopedCompanies()
    {
        using var context = CreateContext();
        context.Companies.Add(new Company { Id = 1, Name = "Alpha", IsActive = true });
        await context.SaveChangesAsync();

        var quota = new Mock<IQuotaControl>();
        quota.Setup(service => service.EnsureAvailableAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<ClaimsPrincipal?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ImportStandardSoftwareCommandHandler(
            context,
            quota.Object,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["Alpha"]));

        var result = await handler.Handle(
            new ImportStandardSoftwareCommand(CreateSoftwareImportFile(
                ["Adobe Acrobat", "FALSE", "TRUE", "Acrobat Pro", "ALPHA-KEY", "Perpetual", "", "1", "FALSE"])),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var license = await context.SoftwareLicenses
            .Include(item => item.SoftwareLicenseCompanies)
            .SingleAsync();
        Assert.Equal([1], license.SoftwareLicenseCompanies.Select(link => link.CompanyId).ToArray());
        var importedKey = await context.SoftwareKeys.SingleAsync();
        Assert.Equal("ALPHA-KEY", importedKey.KeyValue);
        Assert.False(string.IsNullOrWhiteSpace(importedKey.KeyValueHash));

        var duplicateResult = await handler.Handle(
            new ImportStandardSoftwareCommand(CreateSoftwareImportFile(
                ["Adobe Acrobat", "FALSE", "TRUE", "Acrobat Pro", "ALPHA-KEY", "Perpetual", "", "1", "FALSE"])),
            CancellationToken.None);
        Assert.True(duplicateResult.Succeeded);
        Assert.Single(await context.SoftwareKeys.ToListAsync());
    }

    [Fact]
    public async Task ImportStandardSoftwareHandler_DoesNotAddKeyToAnotherCompanyLicense()
    {
        using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });
        context.StandardSoftware.Add(new StandardSoftware
        {
            Id = 10,
            SoftwareName = "Adobe Acrobat",
            Licenses =
            [
                new SoftwareLicense
                {
                    Id = 20,
                    SoftwareName = "Acrobat Pro",
                    SoftwareLicenseCompanies =
                    [
                        new SoftwareLicenseCompany { CompanyId = 2 }
                    ]
                }
            ]
        });
        await context.SaveChangesAsync();

        var quota = new Mock<IQuotaControl>();
        quota.Setup(service => service.EnsureAvailableAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<ClaimsPrincipal?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ImportStandardSoftwareCommandHandler(
            context,
            quota.Object,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["Alpha"]));

        var result = await handler.Handle(
            new ImportStandardSoftwareCommand(CreateSoftwareImportFile(
                ["Adobe Acrobat", "FALSE", "TRUE", "Acrobat Pro", "BETA-SECRET", "Perpetual", "", "1", "FALSE"])),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains("outside your company scope", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await context.SoftwareKeys.ToListAsync());
    }

    private static IFormFile CreateSoftwareImportFile(params string[][] rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Software");
        var headers = new[]
        {
            "SoftwareName", "IsMandatory", "IsActive", "VersionName", "LicenseKey",
            "LicenseType", "ExpiryDate", "Quantity", "IsFree"
        };

        for (var column = 0; column < headers.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                worksheet.Cell(row + 2, column + 1).Value = rows[row][column];
            }
        }

        using var buffer = new MemoryStream();
        workbook.SaveAs(buffer);
        var content = new MemoryStream(buffer.ToArray());
        return new FormFile(content, 0, content.Length, "file", "software-import.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }


    [Fact]
    public async Task ImportStandardSoftwareHandler_RejectsExcessiveGeneratedSubscriptionSeats()
    {
        using var context = CreateContext();
        context.Companies.Add(new Company { Id = 1, Name = "Alpha", IsActive = true });
        await context.SaveChangesAsync();

        var quota = new Mock<IQuotaControl>();
        quota.Setup(service => service.EnsureAvailableAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<ClaimsPrincipal?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ImportStandardSoftwareCommandHandler(
            context,
            quota.Object,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["Alpha"]));

        var result = await handler.Handle(
            new ImportStandardSoftwareCommand(CreateSoftwareImportFile(
                ["Adobe Acrobat", "FALSE", "TRUE", "Acrobat Pro", "KEY-TOO-MANY-SEATS", "Subscription", "31/12/2027", "5001", "FALSE"])),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains("limited to 5,000 seats per import", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await context.SoftwareLicenses.ToListAsync());
        Assert.Empty(await context.SoftwareKeys.ToListAsync());
    }

    [Fact]
    /// <summary>
    /// Runs the ImportStandardSoftwareValidator_Fails_WhenFileMissing operation.
    /// </summary>
    public void ImportStandardSoftwareValidator_Fails_WhenFileMissing()
    {
        var validator = new ImportStandardSoftwareCommandValidator();

        var result = validator.Validate(new ImportStandardSoftwareCommand(null!));

        Assert.False(result.IsValid);
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
    /// Creates data for the CreateCurrentUserAccessor operation.
    /// </summary>
    private static ICurrentUserAccessor CreateCurrentUserAccessor()
    {
        var mock = new Mock<ICurrentUserAccessor>();
        mock.SetupGet(accessor => accessor.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        mock.SetupGet(accessor => accessor.UserName).Returns((string?)null);
        return mock.Object;
    }
}
