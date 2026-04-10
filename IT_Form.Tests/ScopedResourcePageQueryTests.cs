using ClosedXML.Excel;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.Department;
using IT_Form.Application.Features.EmailGroup;
using IT_Form.Application.Features.ServerFolder;
using IT_Form.Application.Features.SharedMailbox;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class ScopedResourcePageQueryTests
{
    public ScopedResourcePageQueryTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    public async Task DepartmentQueries_ReturnTypedIndexAndEditorModels()
    {
        await using var context = CreateContext();

        var alpha = new Company { Id = 1, Name = "Alpha", Lot = "L1", IsActive = true };
        var beta = new Company { Id = 2, Name = "Beta", IsActive = true };
        context.Companies.AddRange(alpha, beta);
        context.Departments.Add(new Department
        {
            Id = 7,
            Name = "Finance",
            IsActive = false,
            DepartmentCompanies =
            [
                new DepartmentCompany { Company = alpha, CompanyId = alpha.Id },
                new DepartmentCompany { Company = beta, CompanyId = beta.Id }
            ]
        });
        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserAccessor();
        var indexHandler = new GetDepartmentIndexPageQueryHandler(context, currentUser);
        var editHandler = new GetDepartmentEditPageQueryHandler(context, currentUser);

        var indexResult = await indexHandler.Handle(new GetDepartmentIndexPageQuery(), CancellationToken.None);
        var editResult = await editHandler.Handle(new GetDepartmentEditPageQuery(7), CancellationToken.None);

        Assert.True(indexResult.IsSuccess);
        var item = Assert.Single(indexResult.Data!);
        Assert.Equal(7, item.Id);
        Assert.Equal("Finance", item.Name);
        Assert.Equal(["Alpha", "Beta"], item.Companies.OrderBy(name => name).ToList());

        Assert.True(editResult.IsSuccess);
        var model = Assert.IsType<DepartmentEditorPageViewModel>(editResult.Data);
        Assert.Equal(7, model.Id);
        Assert.Equal("Finance", model.Name);
        Assert.False(model.IsActive);
        Assert.Equal([1, 2], model.SelectedCompanyIds.OrderBy(id => id).ToList());
        Assert.Equal(["Alpha (L1)", "Beta"], model.CompanyOptions.Select(company => company.DisplayName).ToList());
    }

    [Fact]
    public async Task EmailGroupMembersQuery_ReturnsTypedMembersPage()
    {
        await using var context = CreateContext();

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

        var handler = new GetEmailGroupMembersPageQueryHandler(context, new TestCurrentUserAccessor());
        var result = await handler.Handle(new GetEmailGroupMembersPageQuery(group.Id, onboarding.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<EmailGroupMembersPageViewModel>(result.Data);
        Assert.Equal(group.Id, model.GroupId);
        Assert.Equal("IT Team", model.GroupName);
        Assert.Equal("it.team@test.local", model.GroupEmail);
        Assert.Equal(onboarding.Id, model.CurrentEmployeeId);
        Assert.Equal("Alice Tan (E-11)", model.CurrentEmployeeDisplayName);
        Assert.Contains(model.CurrentEmployees, employee => employee.Id == onboarding.Id && employee.Label == "Alice Tan (E-11)");

        var member = Assert.Single(model.Members);
        Assert.Equal("Alice Tan", member.EmployeeName);
        Assert.Equal("IT", member.Department);
        Assert.Equal("Onboarding", member.RequestType);
        Assert.Equal(RequestStatus.Completed, member.Status);
        Assert.Equal("Onboarding", member.ControllerName);
        Assert.Equal(onboarding.Id, member.RequestId);
    }

    [Fact]
    public async Task SharedMailboxMembersQuery_ReturnsTypedMembersPage()
    {
        await using var context = CreateContext();

        var company = new Company { Id = 1, Name = "Alpha", IsActive = true };
        var mailbox = new SharedMailbox
        {
            Id = 4,
            MailboxName = "Accounts",
            MailboxAddress = "accounts@test.local",
            IsActive = true,
            SharedMailboxCompanies = [new SharedMailboxCompany { Company = company, CompanyId = company.Id }]
        };
        var onboarding = new OnboardingRequest
        {
            Id = 12,
            FullName = "Brian Lee",
            EmployeeID = "E-12",
            Department = "Finance",
            Status = RequestStatus.Approved,
            RequestDate = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc)
        };
        context.Companies.Add(company);
        context.SharedMailboxes.Add(mailbox);
        context.OnboardingRequests.Add(onboarding);
        context.RequestSharedMailboxes.Add(new RequestSharedMailbox
        {
            Id = 22,
            SharedMailbox = mailbox,
            SharedMailboxId = mailbox.Id,
            OnboardingRequest = onboarding,
            OnboardingRequestId = onboarding.Id,
            MailboxName = mailbox.MailboxName,
            MailboxAddress = mailbox.MailboxAddress
        });
        await context.SaveChangesAsync();

        var handler = new GetSharedMailboxMembersPageQueryHandler(context, new TestCurrentUserAccessor());
        var result = await handler.Handle(new GetSharedMailboxMembersPageQuery(mailbox.Id, onboarding.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<SharedMailboxMembersPageViewModel>(result.Data);
        Assert.Equal(mailbox.Id, model.MailboxId);
        Assert.Equal("Accounts", model.MailboxName);
        Assert.Equal("accounts@test.local", model.MailboxAddress);
        Assert.Equal("Brian Lee (E-12)", model.CurrentEmployeeDisplayName);

        var member = Assert.Single(model.Members);
        Assert.Equal("Brian Lee", member.EmployeeName);
        Assert.Equal("Finance", member.Department);
        Assert.Equal("Onboarding", member.RequestType);
        Assert.Equal(RequestStatus.Approved, member.Status);
    }

    [Fact]
    public async Task ServerFolderMembersQuery_ReturnsTypedMembersPageWithPermissionFlags()
    {
        await using var context = CreateContext();

        var company = new Company { Id = 1, Name = "Alpha", IsActive = true };
        var folder = new ServerFolder
        {
            Id = 5,
            FolderName = "Finance_Shared",
            IsActive = true,
            ServerFolderCompanies = [new ServerFolderCompany { Company = company, CompanyId = company.Id }]
        };
        var onboarding = new OnboardingRequest
        {
            Id = 13,
            FullName = "Cara Wong",
            EmployeeID = "E-13",
            Department = "Finance",
            Status = RequestStatus.Completed,
            RequestDate = new DateTime(2026, 4, 3, 8, 0, 0, DateTimeKind.Utc)
        };
        context.Companies.Add(company);
        context.ServerFolders.Add(folder);
        context.OnboardingRequests.Add(onboarding);
        context.FolderPermissions.Add(new FolderPermission
        {
            Id = 23,
            FolderName = folder.FolderName,
            OnboardingRequest = onboarding,
            OnboardingRequestId = onboarding.Id,
            CanRead = true,
            CanModify = true
        });
        await context.SaveChangesAsync();

        var handler = new GetServerFolderMembersPageQueryHandler(context, new TestCurrentUserAccessor());
        var result = await handler.Handle(new GetServerFolderMembersPageQuery(folder.Id, onboarding.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<ServerFolderMembersPageViewModel>(result.Data);
        Assert.Equal(folder.Id, model.FolderId);
        Assert.Equal("Finance_Shared", model.FolderName);
        Assert.Equal("Cara Wong (E-13)", model.CurrentEmployeeDisplayName);

        var member = Assert.Single(model.Members);
        Assert.Equal("Cara Wong", member.EmployeeName);
        Assert.Equal("Finance", member.Department);
        Assert.True(member.CanRead);
        Assert.True(member.CanModify);
        Assert.Equal(RequestStatus.Completed, member.Status);
        Assert.Equal("Onboarding", member.ControllerName);
    }

    [Fact]
    public async Task EmailGroupReportQuery_ReturnsTypedPageAndNotFoundForUnknownGroup()
    {
        await using var context = CreateContext();

        var group = new EmailGroup { Id = 30, GroupName = "Finance Team", GroupEmail = "finance@test.local", IsActive = true };
        var onboarding = new OnboardingRequest
        {
            Id = 40,
            FullName = "Dana Lim",
            EmployeeID = "E-40",
            Department = "Finance",
            Branch = "HQ",
            Status = RequestStatus.Completed,
            CompletedAt = new DateTime(2026, 4, 4, 8, 0, 0, DateTimeKind.Utc)
        };
        context.EmailGroups.Add(group);
        context.OnboardingRequests.Add(onboarding);
        context.RequestEmailGroups.Add(new RequestEmailGroup
        {
            Id = 50,
            EmailGroup = group,
            EmailGroupId = group.Id,
            OnboardingRequest = onboarding,
            OnboardingRequestId = onboarding.Id,
            GroupName = group.GroupName,
            GroupEmail = group.GroupEmail
        });
        await context.SaveChangesAsync();

        var handler = new GetEmailGroupReportPageQueryHandler(context, new TestCurrentUserAccessor());

        var missing = await handler.Handle(new GetEmailGroupReportPageQuery(999, null, 1), CancellationToken.None);
        Assert.True(missing.IsNotFound);

        var result = await handler.Handle(new GetEmailGroupReportPageQuery(group.Id, "Dana", 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<EmailGroupReportPageViewModel>(result.Data);
        Assert.Equal(group.Id, model.GroupId);
        Assert.Equal("Finance Team", model.GroupName);
        Assert.False(model.ShowGroupColumn);
        var item = Assert.Single(model.Report);
        Assert.Equal("Dana Lim", item.EmployeeName);
        Assert.Equal("Finance", item.Department);
        Assert.Equal("finance@test.local", item.GroupEmail);
    }

    [Fact]
    public async Task SharedMailboxExportQuery_ReturnsBinaryFilePayload()
    {
        await using var context = CreateContext();

        var mailbox = new SharedMailbox { Id = 31, MailboxName = "Accounts", MailboxAddress = "accounts@test.local", IsActive = true };
        var onboarding = new OnboardingRequest
        {
            Id = 41,
            FullName = "Evan Goh",
            EmployeeID = "E-41",
            Department = "Accounts",
            Branch = "HQ",
            Status = RequestStatus.Completed,
            CompletedAt = new DateTime(2026, 4, 5, 8, 0, 0, DateTimeKind.Utc)
        };
        context.SharedMailboxes.Add(mailbox);
        context.OnboardingRequests.Add(onboarding);
        context.RequestSharedMailboxes.Add(new RequestSharedMailbox
        {
            Id = 51,
            SharedMailbox = mailbox,
            SharedMailboxId = mailbox.Id,
            OnboardingRequest = onboarding,
            OnboardingRequestId = onboarding.Id,
            MailboxName = mailbox.MailboxName,
            MailboxAddress = mailbox.MailboxAddress
        });
        await context.SaveChangesAsync();

        var handler = new ExportSharedMailboxReportCsvQueryHandler(context, new TestCurrentUserAccessor());
        var result = await handler.Handle(new ExportSharedMailboxReportCsvQuery(mailbox.Id, "Evan"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("text/csv", result.Data!.ContentType);
        Assert.Contains("shared_mailbox_access_report_", result.Data.FileDownloadName);

        var csv = System.Text.Encoding.UTF8.GetString(result.Data.Content);
        Assert.Contains("Employee Name,Department,Branch,Mailbox Name,Mailbox Address,Request Type,Request ID,Date Granted", csv);
        Assert.Contains("Evan Goh", csv);
        Assert.Contains("accounts@test.local", csv);
    }

    [Fact]
    public async Task ServerFolderReportAndTemplateQueries_ReturnTypedResults()
    {
        await using var context = CreateContext();

        var company = new Company { Id = 2, Name = "Beta", Lot = "B2", IsActive = true };
        var folder = new ServerFolder { Id = 32, FolderName = "HR_Share", IsActive = true };
        var onboarding = new OnboardingRequest
        {
            Id = 42,
            FullName = "Fiona Teh",
            EmployeeID = "E-42",
            Department = "HR",
            Branch = "KL",
            Status = RequestStatus.Completed,
            CompletedAt = new DateTime(2026, 4, 6, 8, 0, 0, DateTimeKind.Utc)
        };

        context.Companies.Add(company);
        context.ServerFolders.Add(folder);
        context.OnboardingRequests.Add(onboarding);
        context.FolderPermissions.Add(new FolderPermission
        {
            Id = 52,
            FolderName = folder.FolderName,
            OnboardingRequest = onboarding,
            OnboardingRequestId = onboarding.Id,
            CanRead = true,
            CanModify = false
        });
        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserAccessor();
        var reportHandler = new GetServerFolderReportPageQueryHandler(context, currentUser);
        var templateHandler = new DownloadServerFolderTemplateQueryHandler(context, currentUser);

        var report = await reportHandler.Handle(new GetServerFolderReportPageQuery(folder.Id, "Fiona", 1), CancellationToken.None);
        Assert.True(report.IsSuccess);
        var reportModel = Assert.IsType<ServerFolderReportPageViewModel>(report.Data);
        Assert.Equal(folder.Id, reportModel.FolderId);
        Assert.False(reportModel.ShowFolderColumn);
        Assert.Single(reportModel.Report);
        Assert.Equal("Read Only", reportModel.Report[0].AccessLevel);

        var template = await templateHandler.Handle(new DownloadServerFolderTemplateQuery(), CancellationToken.None);
        Assert.True(template.IsSuccess);
        Assert.NotNull(template.Data);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", template.Data!.ContentType);
        Assert.Equal("ServerFolders_Template.xlsx", template.Data.FileDownloadName);
        Assert.NotEmpty(template.Data.Content);

        using var folderWorkbook = OpenWorkbook(template.Data.Content);
        var folderSheet = folderWorkbook.Worksheet("ServerFolders");
        Assert.Contains(ExcelTemplateDropdownHelper.CompanyOptionsDefinedName, folderSheet.Range("B2:B1000").GetDataValidation().Value);
        Assert.Contains("TRUE,FALSE", folderSheet.Range("C2:C1000").GetDataValidation().Value);

        var folderReference = folderWorkbook.Worksheet("Reference");
        Assert.Equal("Active Company Names", folderReference.Cell(1, 1).GetString());
        Assert.Equal("Beta (B2)", folderReference.Cell(2, 1).GetString());
    }

    [Fact]
    public async Task DepartmentTemplateQuery_ReturnsBinaryFilePayload()
    {
        await using var context = CreateContext();

        context.Companies.Add(new Company { Id = 9, Name = "Gamma", IsActive = true });
        await context.SaveChangesAsync();

        var handler = new DownloadDepartmentTemplateQueryHandler(context, new TestCurrentUserAccessor());
        var result = await handler.Handle(new DownloadDepartmentTemplateQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.Data!.ContentType);
        Assert.Equal("Department_Template.xlsx", result.Data.FileDownloadName);
        Assert.NotEmpty(result.Data.Content);

        using var workbook = OpenWorkbook(result.Data.Content);
        var worksheet = workbook.Worksheet("Departments");
        Assert.Contains(ExcelTemplateDropdownHelper.CompanyOptionsDefinedName, worksheet.Range("C2:C1000").GetDataValidation().Value);
        Assert.Contains("TRUE,FALSE", worksheet.Range("D2:D1000").GetDataValidation().Value);

        var referenceSheet = workbook.Worksheet("Reference");
        Assert.Equal("Active Company Names", referenceSheet.Cell(1, 1).GetString());
        Assert.Equal("Gamma", referenceSheet.Cell(2, 1).GetString());
    }

    [Fact]
    public async Task EmailGroupAndSharedMailboxTemplateQueries_IncludeLiveCompanyDropdowns()
    {
        await using var context = CreateContext();

        context.Companies.AddRange(
            new Company { Id = 70, Name = "Alpha", Lot = "A1", IsActive = true },
            new Company { Id = 71, Name = "Zulu", IsActive = true },
            new Company { Id = 72, Name = "Inactive", IsActive = false });
        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserAccessor();
        var emailGroupHandler = new DownloadEmailGroupTemplateQueryHandler(context, currentUser);
        var sharedMailboxHandler = new DownloadSharedMailboxTemplateQueryHandler(context, currentUser);

        var emailGroupTemplate = await emailGroupHandler.Handle(new DownloadEmailGroupTemplateQuery(), CancellationToken.None);
        var sharedMailboxTemplate = await sharedMailboxHandler.Handle(new DownloadSharedMailboxTemplateQuery(), CancellationToken.None);

        Assert.True(emailGroupTemplate.IsSuccess);
        Assert.True(sharedMailboxTemplate.IsSuccess);

        using var emailGroupWorkbook = OpenWorkbook(emailGroupTemplate.Data!.Content);
        var emailGroupSheet = emailGroupWorkbook.Worksheet("EmailGroups");
        Assert.Contains(ExcelTemplateDropdownHelper.CompanyOptionsDefinedName, emailGroupSheet.Range("C2:C1000").GetDataValidation().Value);
        Assert.Contains("TRUE,FALSE", emailGroupSheet.Range("D2:D1000").GetDataValidation().Value);
        Assert.Equal("Alpha (A1)", emailGroupWorkbook.Worksheet("Reference").Cell(2, 1).GetString());
        Assert.Equal("Zulu", emailGroupWorkbook.Worksheet("Reference").Cell(3, 1).GetString());

        using var sharedMailboxWorkbook = OpenWorkbook(sharedMailboxTemplate.Data!.Content);
        var sharedMailboxSheet = sharedMailboxWorkbook.Worksheet("SharedMailboxes");
        Assert.Contains(ExcelTemplateDropdownHelper.CompanyOptionsDefinedName, sharedMailboxSheet.Range("C2:C1000").GetDataValidation().Value);
        Assert.Contains("TRUE,FALSE", sharedMailboxSheet.Range("D2:D1000").GetDataValidation().Value);
        Assert.Equal("Alpha (A1)", sharedMailboxWorkbook.Worksheet("Reference").Cell(2, 1).GetString());
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

    private static XLWorkbook OpenWorkbook(byte[] content)
        => new(new MemoryStream(content));
}
