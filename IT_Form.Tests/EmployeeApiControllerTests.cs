// Project comment: EmployeeApiControllerTests.cs is part of the IT Form application source code.

using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using IT_Form.Controllers;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the EmployeeApiControllerTests class used by the IT Form application.
    /// </summary>
    public class EmployeeApiControllerTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Runs the Import_RecordsEmployeeIdDeskNumberAndJoinDate_FromHeaderMappedSheet operation.
        /// </summary>
        public async Task Import_RecordsEmployeeIdDeskNumberAndJoinDate_FromHeaderMappedSheet()
        {
            using var context = CreateContext();
            context.Companies.Add(new Company
            {
                Id = 1,
                Name = "YTC PRECAST SDN BHD",
                Lot = "LOT897",
                IsActive = true
            });
            await context.SaveChangesAsync();

            using var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Staff Template");
                sheet.Cell(1, 1).Value = "Full Name";
                sheet.Cell(1, 2).Value = "Employee ID";
                sheet.Cell(1, 3).Value = "Date Hire";
                sheet.Cell(1, 4).Value = "Desk Number";
                sheet.Cell(1, 5).Value = "Position";
                sheet.Cell(1, 6).Value = "Email";
                sheet.Cell(1, 7).Value = "Department";
                sheet.Cell(1, 8).Value = "Contact Number";
                sheet.Cell(1, 9).Value = "Reporting Manager";
                sheet.Cell(1, 10).Value = "Company";

                sheet.Cell(2, 1).Value = "ZAIDI BIN AHMAD";
                sheet.Cell(2, 2).Value = "EMP-001";
                sheet.Cell(2, 3).Value = "03/04/2026";
                sheet.Cell(2, 4).Value = "D-17";
                sheet.Cell(2, 5).Value = "MANAGER";
                sheet.Cell(2, 6).Value = "zaidi@ytcprecast.com";
                sheet.Cell(2, 7).Value = "SAFETY";
                sheet.Cell(2, 8).Value = "0123456789";
                sheet.Cell(2, 9).Value = "michael.phuah@ytcprecast.com";
                sheet.Cell(2, 10).Value = "YTC PRECAST SDN BHD (LOT897)";

                workbook.SaveAs(stream);
            }

            stream.Position = 0;
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "staff-import.xlsx") { Headers = new HeaderDictionary(), ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };

            var controller = GetController<EmployeeApiController>(context, role: "HR", branch: "YTC PRECAST SDN BHD");

            var result = await controller.Import(file);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var savedEmployee = await context.OnboardingRequests.SingleAsync();
            Assert.Equal("EMP-001", savedEmployee.EmployeeID);
            Assert.Equal("D-17", savedEmployee.DeskNumber);
            Assert.Equal(new System.DateTime(2026, 4, 3), savedEmployee.JoinDate);
            Assert.Equal("YTC PRECAST SDN BHD (LOT897)", savedEmployee.Branch);
            Assert.Equal("testuser", savedEmployee.CreatedByUserId);
            Assert.Equal("test-signature", savedEmployee.RequesterSignature);
            Assert.Equal("testuser", savedEmployee.RequesterSignerName);
            Assert.NotNull(savedEmployee.RequesterSignedAt);
            Assert.Equal("HMAC", savedEmployee.RequesterSignatureHmac);
        }

        [Fact]
        /// <summary>
        /// Runs the Import_MatchesCompanyFromPartialNameAndLotReference operation.
        /// </summary>
        public async Task Import_MatchesCompanyFromPartialNameAndLotReference()
        {
            using var context = CreateContext();
            context.Companies.AddRange(
                new Company
                {
                    Id = 1,
                    Name = "YTC Precast",
                    Lot = "LOT897",
                    IsActive = true
                },
                new Company
                {
                    Id = 2,
                    Name = "YTC Transportation",
                    Lot = "LOT123",
                    IsActive = true
                });
            await context.SaveChangesAsync();

            using var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Staff Template");
                sheet.Cell(1, 1).Value = "Full Name";
                sheet.Cell(1, 2).Value = "Employee ID";
                sheet.Cell(1, 3).Value = "Date Hire";
                sheet.Cell(1, 4).Value = "Desk Number";
                sheet.Cell(1, 5).Value = "Position";
                sheet.Cell(1, 6).Value = "Email";
                sheet.Cell(1, 7).Value = "Department";
                sheet.Cell(1, 8).Value = "Contact Number";
                sheet.Cell(1, 9).Value = "Reporting Manager";
                sheet.Cell(1, 10).Value = "Company";

                sheet.Cell(2, 1).Value = "ZAIDI BIN AHMAD";
                sheet.Cell(2, 2).Value = "EMP-002";
                sheet.Cell(2, 3).Value = "03/04/2026";
                sheet.Cell(2, 4).Value = "D-18";
                sheet.Cell(2, 5).Value = "MANAGER";
                sheet.Cell(2, 6).Value = "zaidi2@ytcprecast.com";
                sheet.Cell(2, 7).Value = "SAFETY";
                sheet.Cell(2, 8).Value = "0123456789";
                sheet.Cell(2, 9).Value = "michael.phuah@ytcprecast.com";
                sheet.Cell(2, 10).Value = "Precast 897";

                workbook.SaveAs(stream);
            }

            stream.Position = 0;
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "staff-import.xlsx") { Headers = new HeaderDictionary(), ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };

            var controller = GetController<EmployeeApiController>(context, role: "HR", branch: "YTC Precast");

            var result = await controller.Import(file);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var savedEmployee = await context.OnboardingRequests.SingleAsync();
            Assert.Equal("EMP-002", savedEmployee.EmployeeID);
            Assert.Equal("YTC Precast (LOT897)", savedEmployee.Branch);
        }

        [Fact]
        /// <summary>
        /// Gets the data required by the GetHodsByCompany_ReturnsOnlyCompanyMatchedHods operation.
        /// </summary>
        public async Task GetHodsByCompany_ReturnsOnlyCompanyMatchedHods()
        {
            using var context = CreateContext();

            var hodA = new ApplicationUser { Id = "hod-a", UserName = "hoda", Email = "hoda@test.com", FullName = "HOD A", IsActive = true };
            var hodB = new ApplicationUser { Id = "hod-b", UserName = "hodb", Email = "hodb@test.com", FullName = "HOD B", IsActive = true };
            context.Users.AddRange(hodA, hodB);
            context.UserTitles.AddRange(
                new UserTitle { UserId = hodA.Id, Title = "HOD" },
                new UserTitle { UserId = hodB.Id, Title = "HOD" }
            );
            context.UserCompanies.AddRange(
                new UserCompany { UserId = hodA.Id, CompanyName = "Alpha" },
                new UserCompany { UserId = hodB.Id, CompanyName = "Beta" }
            );
            await context.SaveChangesAsync();

            var controller = GetController<EmployeeApiController>(context, role: "HR", branch: "YTC PRECAST SDN BHD");
            var result = await controller.GetHodsByCompany("Alpha (Lot 1)");

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            Assert.Contains("hoda@test.com", json);
            Assert.DoesNotContain("hodb@test.com", json);
        }

        [Fact]
        /// <summary>
        /// Runs the DownloadTemplate_UsesCurrentDepartmentAndCompanyDropdowns operation.
        /// </summary>
        public async Task DownloadTemplate_UsesCurrentDepartmentAndCompanyDropdowns()
        {
            using var context = CreateContext();
            context.Departments.AddRange(
                new Department { Id = 1, Name = "Finance", IsActive = true },
                new Department { Id = 2, Name = "Inactive Department", IsActive = false });
            context.Companies.AddRange(
                new Company { Id = 1, Name = "YTC PRECAST SDN BHD", Lot = "LOT897", IsActive = true },
                new Company { Id = 2, Name = "Inactive Company", IsActive = false });
            await context.SaveChangesAsync();

            var controller = GetController<EmployeeApiController>(context, role: "HR", branch: "YTC PRECAST SDN BHD");

            var result = await controller.DownloadTemplate();

            var file = Assert.IsType<FileContentResult>(result);
            using var workbook = new XLWorkbook(new MemoryStream(file.FileContents));
            var worksheet = workbook.Worksheet("Staff Template");

            Assert.Contains(ExcelTemplateDropdownHelper.DepartmentOptionsDefinedName, worksheet.Range("G2:G1000").GetDataValidation().Value);
            Assert.Contains(ExcelTemplateDropdownHelper.CompanyOptionsDefinedName, worksheet.Range("J2:J1000").GetDataValidation().Value);

            var refSheet = workbook.Worksheet("_Ref");
            Assert.Equal("Active Departments", refSheet.Cell(1, 1).GetString());
            Assert.Equal("Finance", refSheet.Cell(2, 1).GetString());
            Assert.Equal("Active Companies", refSheet.Cell(1, 2).GetString());
            Assert.Equal("YTC PRECAST SDN BHD (LOT897)", refSheet.Cell(2, 2).GetString());
        }
    }
}
