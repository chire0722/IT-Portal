using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Application.Features.ServerFolder;
using IT_Form.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    public class ServerFolderControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.ServerFolders.Add(new IT_Form.Models.ServerFolder { FolderName = "Public", IsActive = true });
            await context.SaveChangesAsync();

            var controller = GetController<ServerFolderController>(context, role: "IT");
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ServerFolderIndexListItem>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_Post_ReturnsView_WhenModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<ServerFolderController>(context);

            var result = await controller.Create((ServerFolderEditorPageViewModel)null!);

            Assert.IsType<ViewResult>(result);
        }

        // ServerFolderController does not have a Details action in this version
    }
}
