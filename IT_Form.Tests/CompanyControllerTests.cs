using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Application.Features.Company;
using IT_Form.Controllers;
using IT_Form.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    public class CompanyControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.Companies.Add(new Company { Name = "BranchA", IsActive = true });
            await context.SaveChangesAsync();

            var controller = GetController<CompanyController>(context, role: "IT");
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<CompanyIndexListItem>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_Post_ReturnsView_WhenModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<CompanyController>(context);

            var result = await controller.Create((CompanyEditorPageViewModel)null!);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Edit_Post_GuardAgainstTampering_Adversarial()
        {
            using var context = CreateContext();
            var company = new Company { Name = "Original", IsActive = true };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var controller = GetController<CompanyController>(context);

            var result = await controller.Edit(company.Id + 1, new CompanyEditorPageViewModel
            {
                Id = company.Id,
                Name = company.Name,
                Lot = company.Lot,
                LogoPath = company.LogoPath,
                IsLoginPageLogo = company.IsLoginPageLogo,
                IsActive = company.IsActive
            });

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
