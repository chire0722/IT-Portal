using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Application.Features.EmailGroup;
using IT_Form.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    public class EmailGroupControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.EmailGroups.Add(new IT_Form.Models.EmailGroup { GroupName = "IT_Team", IsActive = true });
            await context.SaveChangesAsync();

            var controller = GetController<EmailGroupController>(context, role: "IT");
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<EmailGroupIndexListItem>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
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
