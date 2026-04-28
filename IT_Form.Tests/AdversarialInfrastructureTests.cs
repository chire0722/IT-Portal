// Project comment: AdversarialInfrastructureTests.cs is part of the IT Form application source code.

using System;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Data;
using IT_Form.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    // A dummy controller to test the base infrastructure
    /// <summary>
    /// Handles web requests for the MockSecurity area.
    /// </summary>
    public class MockSecurityController : Controller
    {
        private readonly ApplicationDbContext _context;
        public MockSecurityController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Runs the AdminOnly operation.
        /// </summary>
        public IActionResult AdminOnly()
        {
            if (User.FindFirst("UserTitle")?.Value != "IT") return Forbid();
            return Ok("Secret Area");
        }

        /// <summary>
        /// Runs the NullCheck operation.
        /// </summary>
        public IActionResult NullCheck(Department? dept)
        {
            if (dept == null) return BadRequest("Null provided");
            return Ok(dept.Name);
        }

        /// <summary>
        /// Runs the Details operation.
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            return Ok(dept.Name);
        }
    }

    /// <summary>
    /// Defines the AdversarialInfrastructureTests class used by the IT Form application.
    /// </summary>
    public class AdversarialInfrastructureTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Runs the Base_CanSimulate_AdminRole operation.
        /// </summary>
        public void Base_CanSimulate_AdminRole()
        {
            using var context = CreateContext();
            var controller = GetController<MockSecurityController>(context, role: "IT");

            var result = controller.AdminOnly();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the Base_CanSimulate_UnauthorizedRole operation.
        /// </summary>
        public void Base_CanSimulate_UnauthorizedRole()
        {
            using var context = CreateContext();
            var controller = GetController<MockSecurityController>(context, role: "HR");

            var result = controller.AdminOnly();

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the Base_Supports_NullInjection operation.
        /// </summary>
        public void Base_Supports_NullInjection()
        {
            using var context = CreateContext();
            var controller = GetController<MockSecurityController>(context);

            var result = controller.NullCheck(null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Null provided", badRequest.Value);
        }

        [Fact]
        /// <summary>
        /// Runs the Base_Handles_MissingId_WithoutCrash operation.
        /// </summary>
        public async Task Base_Handles_MissingId_WithoutCrash()
        {
            using var context = CreateContext();
            var controller = GetController<MockSecurityController>(context);

            // Attempting to access ID 999 which doesn't exist
            var result = await controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        /// <summary>
        /// Runs the Base_Session_IsOperation operation.
        /// </summary>
        public void Base_Session_IsOperation()
        {
             using var context = CreateContext();
             var controller = GetController<MockSecurityController>(context);
             
             Assert.NotNull(controller.HttpContext.Session);
             controller.HttpContext.Session.SetString("Test", "Value");
             Assert.Equal("Value", controller.HttpContext.Session.GetString("Test"));
        }
    }
}
