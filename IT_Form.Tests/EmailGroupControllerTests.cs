// Project comment: EmailGroupControllerTests.cs is part of the IT Form application source code.

using System.Threading.Tasks;
using IT_Form.Application.Features.EmailGroup;
using IT_Form.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the EmailGroupControllerTests class used by the IT Form application.
    /// </summary>
    public class EmailGroupControllerTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Runs the Index_ReturnsView_ForITRole operation.
        /// </summary>
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.EmailGroups.Add(new IT_Form.Models.EmailGroup { GroupName = "IT_Team", IsActive = true });
            await context.SaveChangesAsync();

            var controller = GetController<EmailGroupController>(context, role: "IT");
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EmailGroupIndexPageViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.Items);
        }

        [Fact]
        /// <summary>
        /// Creates data for the Create_Post_ReturnsView_WhenModelIsNull_Adversarial operation.
        /// </summary>
        public async Task Create_Post_ReturnsView_WhenModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<EmailGroupController>(context);

            var result = await controller.Create((EmailGroupEditorPageViewModel)null!);

            Assert.IsType<ViewResult>(result);
        }

        // EmailGroupController does not have a Details action
    }
}
