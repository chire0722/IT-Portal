// Project comment: AuditLogControllerTests.cs is part of the IT Form application source code.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Models;
using IT_Form.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the AuditLogControllerTests class used by the IT Form application.
    /// </summary>
    public class AuditLogControllerTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Runs the Index_ReturnsView_ForITRole operation.
        /// </summary>
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.AuditLogs.Add(new AuditLog { Action = "Test", PerformedBy = "it@ytc.com", Timestamp = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var controller = GetController<AuditLogController>(context, role: "IT");
            var result = await controller.Index(null, null, null, null, null, null, 1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AuditLogIndexPageViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.Entries);
        }

        [Fact]
        /// <summary>
        /// Runs the AuditLogController_RequiresITPolicy operation.
        /// </summary>
        public void AuditLogController_RequiresITPolicy()
        {
            var attribute = Assert.Single(
                typeof(AuditLogController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                    .Cast<AuthorizeAttribute>());

            Assert.Equal("RequireIT", attribute.Policy);
        }

        [Fact]
        public async Task ExportCsv_NeutralizesFormulaLikeAuditFields_AndQuotesCsvCells()
        {
            using var context = CreateContext();
            context.AuditLogs.Add(new AuditLog
            {
                UserId = "user-1",
                PerformedBy = "auditor@example.com",
                Action = "Read",
                EntityName = "ITAsset",
                EntityId = "42",
                Details = "=HYPERLINK(\"https://example.invalid\")",
                ErrorMessage = "+SUM(1,2)",
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsSuccess = true
            });
            await context.SaveChangesAsync();

            var controller = GetController<AuditLogController>(context, role: "IT");
            var result = await controller.ExportCsv(null, null, null, null, null, null);

            var file = Assert.IsType<FileContentResult>(result);
            var csv = System.Text.Encoding.UTF8.GetString(file.FileContents);
            Assert.Contains("\"'=HYPERLINK(\"\"https://example.invalid\"\")\"", csv);
            Assert.Contains("\"'+SUM(1,2)\"", csv);
        }

    }
}
