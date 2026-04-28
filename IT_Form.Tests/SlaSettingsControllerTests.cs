// Project comment: SlaSettingsControllerTests.cs is part of the IT Form application source code.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the SlaSettingsControllerTests class used by the IT Form application.
    /// </summary>
    public class SlaSettingsControllerTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Runs the Index_ReturnsDefaultRows_WhenTableIsEmpty operation.
        /// </summary>
        public async Task Index_ReturnsDefaultRows_WhenTableIsEmpty()
        {
            using var context = CreateContext();

            var controller = GetController<SlaSettingsController>(context, role: "IT");
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<SlaSetting>>(viewResult.ViewData.Model).ToList();
            Assert.Equal(2, model.Count);
            Assert.Contains(model, s => s.RequestType == "Onboarding");
            Assert.Contains(model, s => s.RequestType == "Offboarding");
        }

        [Fact]
        /// <summary>
        /// Runs the Index_AddsMissingDefaultRows_WithoutReplacingExistingValues operation.
        /// </summary>
        public async Task Index_AddsMissingDefaultRows_WithoutReplacingExistingValues()
        {
            using var context = CreateContext();
            context.SlaSettings.Add(new SlaSetting { RequestType = "Onboarding", TargetDays = 5 });
            await context.SaveChangesAsync();

            var controller = GetController<SlaSettingsController>(context, role: "IT");
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<SlaSetting>>(viewResult.ViewData.Model).ToList();
            Assert.Equal(2, model.Count);
            Assert.Contains(model, s => s.RequestType == "Onboarding" && s.TargetDays == 5);
        }

        // SlaSettingsController only has Index and Update
        
        [Fact]
        /// <summary>
        /// Runs the Index_Forbidden_ForHR_Adversarial operation.
        /// </summary>
        public async Task Index_Forbidden_ForHR_Adversarial()
        {
            // SLA Settings should be IT-only
            using var context = CreateContext();
            var controller = GetController<SlaSettingsController>(context, role: "HR");
            var result = await controller.Index();

            Assert.IsType<ForbidResult>(result);
        }
    }
}
