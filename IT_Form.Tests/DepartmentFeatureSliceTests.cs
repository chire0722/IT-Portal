// Project comment: DepartmentFeatureSliceTests.cs is part of the IT Form application source code.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.Department.ImportDepartments;
using IT_Form.Application.Features.Departments.Writes.CreateDepartment;
using IT_Form.Application.Features.Departments.Writes.DeleteDepartment;
using IT_Form.Application.Features.Departments.Writes.UpdateDepartment;
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
/// Defines the DepartmentFeatureSliceTests class used by the IT Form application.
/// </summary>
public class DepartmentFeatureSliceTests
{
    public DepartmentFeatureSliceTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateDepartmentValidator_Fails_WhenNameMissing operation.
    /// </summary>
    public async Task CreateDepartmentValidator_Fails_WhenNameMissing()
    {
        using var context = CreateContext();
        var validator = new CreateDepartmentCommandValidator(context);

        var result = await validator.ValidateAsync(
            new CreateDepartmentCommand(string.Empty, null, true, Array.Empty<int>()),
            CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateDepartmentValidator_Fails_WhenNameIsDuplicate operation.
    /// </summary>
    public async Task CreateDepartmentValidator_Fails_WhenNameIsDuplicate()
    {
        using var context = CreateContext();
        context.Departments.Add(new Department { Name = "IT" });
        await context.SaveChangesAsync();

        var validator = new CreateDepartmentCommandValidator(context);
        var result = await validator.ValidateAsync(
            new CreateDepartmentCommand("IT", "Duplicate", true, Array.Empty<int>()),
            CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateDepartmentHandler_CreatesDepartment_WithDescriptionAndCompanies operation.
    /// </summary>
    public async Task CreateDepartmentHandler_CreatesDepartment_WithDescriptionAndCompanies()
    {
        using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });
        await context.SaveChangesAsync();

        var handler = new CreateDepartmentCommandHandler(context, new TestCurrentUserAccessor());

        var id = await handler.Handle(
            new CreateDepartmentCommand("Finance", "Handles finance operations.", true, new[] { 1, 2, 2 }),
            CancellationToken.None);

        var entity = await context.Departments
            .Include(department => department.DepartmentCompanies)
            .FirstAsync(department => department.Id == id);

        Assert.Equal("Finance", entity.Name);
        Assert.Equal("Handles finance operations.", entity.Description);
        Assert.Equal(2, entity.DepartmentCompanies.Count);
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateDepartmentHandler_AllowsItUserToAssignAcrossCompanies operation.
    /// </summary>
    public async Task CreateDepartmentHandler_AllowsItUserToAssignAcrossCompanies()
    {
        using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });
        await context.SaveChangesAsync();

        var handler = new CreateDepartmentCommandHandler(
            context,
            new TestCurrentUserAccessor(titles: ["IT"], companies: ["Alpha"]));

        var id = await handler.Handle(
            new CreateDepartmentCommand("Regional Ops", null, true, [2]),
            CancellationToken.None);

        var entity = await context.Departments
            .Include(department => department.DepartmentCompanies)
            .SingleAsync(department => department.Id == id);

        Assert.Equal("Regional Ops", entity.Name);
        Assert.Equal([2], entity.DepartmentCompanies.Select(link => link.CompanyId).ToList());
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateDepartmentHandler_AllowsGlobalAccessUserToCreateGlobalDepartment operation.
    /// </summary>
    public async Task CreateDepartmentHandler_AllowsGlobalAccessUserToCreateGlobalDepartment()
    {
        using var context = CreateContext();

        var handler = new CreateDepartmentCommandHandler(
            context,
            new TestCurrentUserAccessor(titles: ["IT"], companies: ["Alpha"]));

        var id = await handler.Handle(
            new CreateDepartmentCommand("Global Support", null, true, []),
            CancellationToken.None);

        var entity = await context.Departments
            .Include(department => department.DepartmentCompanies)
            .SingleAsync(department => department.Id == id);

        Assert.Equal("Global Support", entity.Name);
        Assert.Empty(entity.DepartmentCompanies);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateDepartmentHandler_ThrowsNotFound_WhenDepartmentMissing operation.
    /// </summary>
    public async Task UpdateDepartmentHandler_ThrowsNotFound_WhenDepartmentMissing()
    {
        using var context = CreateContext();
        var handler = new UpdateDepartmentCommandHandler(context, new TestCurrentUserAccessor());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateDepartmentCommand(999, "IT", "Missing", true, Array.Empty<int>()),
                CancellationToken.None));
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteDepartmentHandler_DeletesDepartment_WhenFound operation.
    /// </summary>
    public async Task DeleteDepartmentHandler_DeletesDepartment_WhenFound()
    {
        using var context = CreateContext();
        context.Departments.Add(new Department
        {
            Id = 1,
            Name = "To Delete",
            Description = "Legacy",
            DepartmentCompanies = { new DepartmentCompany { CompanyId = 10 } }
        });
        await context.SaveChangesAsync();

        var handler = new DeleteDepartmentCommandHandler(context, new TestCurrentUserAccessor());
        await handler.Handle(new DeleteDepartmentCommand(1), CancellationToken.None);

        Assert.Null(await context.Departments.FindAsync(1));
    }

    [Fact]
    /// <summary>
    /// Runs the ImportDepartmentsValidator_Fails_WhenFileMissing operation.
    /// </summary>
    public void ImportDepartmentsValidator_Fails_WhenFileMissing()
    {
        var validator = new ImportDepartmentsCommandValidator();

        var result = validator.Validate(new ImportDepartmentsCommand(null!));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the ImportDepartmentsHandler_CreatesDepartment_WithDescription_FromNewTemplate operation.
    /// </summary>
    public async Task ImportDepartmentsHandler_CreatesDepartment_WithDescription_FromNewTemplate()
    {
        using var context = CreateContext();
        context.Companies.Add(new Company { Id = 1, Name = "All Company", IsActive = true });
        await context.SaveChangesAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Departments");
        worksheet.Cell(1, 1).Value = "Name";
        worksheet.Cell(1, 2).Value = "Description";
        worksheet.Cell(1, 3).Value = "Company (Comma-separated for multiple)";
        worksheet.Cell(1, 4).Value = "IsActive (TRUE/FALSE)";
        worksheet.Cell(2, 1).Value = "Finance";
        worksheet.Cell(2, 2).Value = "Handles finance operations.";
        worksheet.Cell(2, 3).Value = "All Company";
        worksheet.Cell(2, 4).Value = "TRUE";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "departments.xlsx");
        var handler = new ImportDepartmentsCommandHandler(context, new TestCurrentUserAccessor());

        var result = await handler.Handle(new ImportDepartmentsCommand(file), CancellationToken.None);

        Assert.True(result.Succeeded);

        var department = await context.Departments.SingleAsync();
        Assert.Equal("Finance", department.Name);
        Assert.Equal("Handles finance operations.", department.Description);
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
}
