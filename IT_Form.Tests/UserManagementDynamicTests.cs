// Project comment: UserManagementDynamicTests.cs is part of the IT Form application source code.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Application.Features.UserManagement;
using CreateUserManagementWriteCommand = IT_Form.Application.Features.UserManagement.Writes.CreateUserManagement.CreateUserManagementCommand;
using IT_Form.ViewModels;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;
using MediatR;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the UserManagementDynamicTests class used by the IT Form application.
    /// </summary>
    public class UserManagementDynamicTests
    {
        /// <summary>
        /// Gets the data required by the GetContext operation.
        /// </summary>
        private ApplicationDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
                
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockDbLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ApplicationDbContext>>();
            var mockLicenseEncryption = new Mock<ILicenseEncryptionService>();

            return new ApplicationDbContext(options, mockHttpContextAccessor.Object, mockDbLogger.Object, mockLicenseEncryption.Object);
        }

        /// <summary>
        /// Gets the data required by the GetController operation.
        /// </summary>
        private UserManagementController GetController(ApplicationDbContext context)
        {
            var sender = new Mock<ISender>();
            sender.Setup(s => s.Send(It.IsAny<CreateUserManagementWriteCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateUserManagementWriteCommand cmd, CancellationToken _) =>
                {
                    var user = new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = cmd.Username,
                        Email = cmd.Email,
                        FullName = cmd.FullName,
                        Department = cmd.Department,
                        IsActive = cmd.IsActive
                    };

                    context.Users.Add(user);
                    foreach (var company in cmd.SelectedCompanies ?? Enumerable.Empty<string>())
                    {
                        context.UserCompanies.Add(new UserCompany
                        {
                            UserId = user.Id,
                            CompanyName = company
                        });
                    }

                    context.SaveChanges();
                    return user.Id;
                });

            var controller = new UserManagementController(sender.Object);
            
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            return controller;
        }

        [Fact]
        /// <summary>
        /// Creates data for the Create_Post_AssignsMultipleCompanies operation.
        /// </summary>
        public async Task Create_Post_AssignsMultipleCompanies()
        {
            using var context = GetContext();
            context.Companies.Add(new Company { Name = "Alpha", IsActive = true });
            context.Companies.Add(new Company { Name = "Beta", IsActive = true });
            await context.SaveChangesAsync();

            var controller = GetController(context);
            var model = new UserManagementEditorPageViewModel
            {
                Username = "newguy",
                Email = "newguy@test.com",
                FullName = "New Guy",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                SelectedCompanies = new List<string> { "Alpha", "Beta" },
                Companies =
                [
                    new UserManagementCompanyOption { DisplayName = "Alpha" },
                    new UserManagementCompanyOption { DisplayName = "Beta" }
                ]
            };

            var result = await controller.Create(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            
            // Verify database mapping directly in UserCompanies table
            var mappings = await context.UserCompanies.ToListAsync();
            Assert.Equal(2, mappings.Count);
            Assert.Contains(mappings, c => c.CompanyName == "Alpha");
            Assert.Contains(mappings, c => c.CompanyName == "Beta");
        }
    }
}
