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
    public class MaintenanceLogControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task Create_Post_ReturnsView_WhenModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<MaintenanceLogController>(context);

            var result = await controller.Create(null!);

            Assert.IsType<ViewResult>(result);
        }

        // MaintenanceLogController does not have a Details action
    }
}
