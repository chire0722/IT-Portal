// Project comment: ServerFolderControllerTests.cs is part of the IT Form application source code.

using System.Threading.Tasks;
using IT_Form.Application.Features.ServerFolder;
using IT_Form.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the ServerFolderControllerTests class used by the IT Form application.
    /// </summary>
    public class ServerFolderControllerTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Runs the Index_ReturnsView_ForITRole operation.
        /// </summary>
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.ServerFolders.Add(new IT_Form.Models.ServerFolder { FolderName = "Public", IsActive = true });
            await context.SaveChangesAsync();

            var controller = GetController<ServerFolderController>(context, role: "IT");
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ServerFolderIndexPageViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.Items);
        }

        [Fact]
        /// <summary>
        /// Creates data for the Create_Post_ReturnsView_WhenModelIsNull_Adversarial operation.
        /// </summary>
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
