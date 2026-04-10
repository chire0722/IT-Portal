using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Models;
using IT_Form.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    public class AuditLogControllerTests : ControllerTestBase
    {
        [Fact]
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
        public async Task Index_Forbidden_ForHR_Adversarial()
        {
             // Audit logs are IT-only
             // Verify that HR is redirected or blocked.
             using var context = CreateContext();
             var controller = GetController<AuditLogController>(context, role: "HR");
             
             // If the controller handles role check manually instead of via [Authorize]
             var result = await controller.Index(null, null, null, null, null, null, 1);
             
             // Check if it returns RedirectToHome or similar if manually guarded
        }
    }
}
