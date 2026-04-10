using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Application.Features.Department;
using IT_Form.Application.Features.Departments.Writes.DeleteDepartment;
using IT_Form.Controllers;
using IT_Form.Data;
using IT_Form.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    public class DepartmentControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.Departments.Add(new Department { Name = "IT", IsActive = true });
            await context.SaveChangesAsync();

            var controller = GetController<DepartmentController>(context, role: "IT");
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<DepartmentIndexListItem>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        // DepartmentController does not have a Details action


        [Fact]
        public async Task Create_Post_ReturnsView_WhenModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<DepartmentController>(context);

            // Attack with NULL model
            var result = await controller.Create(null!);

            // Should not crash (NRE)
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Edit_Post_GuardAgainstTampering_Adversarial()
        {
            using var context = CreateContext();
            var dept = new Department { Name = "Original", IsActive = true };
            context.Departments.Add(dept);
            await context.SaveChangesAsync();

            var controller = GetController<DepartmentController>(context);
            
            // Attack: ID in URL doesn't match ID in Model
            var result = await controller.Edit(dept.Id + 1, new DepartmentEditorPageViewModel
            {
                Id = dept.Id,
                Name = dept.Name
            });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_RemovesDepartment_ForITRole()
        {
            using var context = CreateContext();
            var dept = new Department { Name = "To Delete", IsActive = true };
            context.Departments.Add(dept);
            await context.SaveChangesAsync();

            var sender = new Mock<IMediator>();
            sender.Setup(s => s.Send(It.IsAny<DeleteDepartmentCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DeleteDepartmentCommand cmd, CancellationToken _) =>
                {
                    var found = context.Departments.Find(cmd.Id);
                    if (found != null)
                    {
                        context.Departments.Remove(found);
                        context.SaveChanges();
                    }
                    return Unit.Value;
                });

            var controller = new DepartmentController(sender.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                controller.HttpContext,
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            await controller.Delete(dept.Id);

            var saved = await context.Departments.FindAsync(dept.Id);
            Assert.Null(saved);
        }
    }
}
