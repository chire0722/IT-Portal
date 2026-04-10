using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Data;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.EmployeeTransfer.Edit;
using IT_Form.Application.Features.EmployeeTransfer.Shared;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;
using MediatR;

namespace IT_Form.Tests
{
    public class TransferIntegrityTests
    {
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

        private EmployeeTransferController GetController(ApplicationDbContext context)
        {
            var sender = new Mock<ISender>();
            sender.Setup(s => s.Send(It.IsAny<GetEmployeeTransferEditPageQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetEmployeeTransferEditPageQuery cmd, CancellationToken _) =>
                {
                    var transfer = context.EmployeeTransfers.Find(cmd.Id);
                    if (transfer == null)
                    {
                        return ProcessResult<EmployeeTransferEditPageViewModel>.NotFound();
                    }

                    return transfer.Status == RequestStatus.Completed || transfer.Status == RequestStatus.PendingEmployeeSign
                        ? ProcessResult<EmployeeTransferEditPageViewModel>.Failure("Locked", "Details", cmd.Id)
                        : ProcessResult<EmployeeTransferEditPageViewModel>.Success(new EmployeeTransferEditPageViewModel
                        {
                            Id = transfer.Id,
                            FullName = transfer.FullName,
                            FromDepartment = transfer.FromDepartment,
                            ToDepartment = transfer.ToDepartment
                        });
                });
            sender.Setup(s => s.Send(It.IsAny<SubmitEmployeeTransferEditCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SubmitEmployeeTransferEditCommand cmd, CancellationToken _) =>
                {
                    var transfer = context.EmployeeTransfers.Find(cmd.Id);
                    if (transfer == null)
                    {
                        return ProcessResult<object>.NotFound();
                    }

                    return transfer.Status == RequestStatus.Completed || transfer.Status == RequestStatus.PendingEmployeeSign
                        ? ProcessResult<object>.Failure("Locked", "Details", cmd.Id)
                        : ProcessResult<object>.Success(redirectAction: "Edit", requestId: cmd.Id);
                });

            var controller = new EmployeeTransferController(sender.Object);
            
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "ituser"),
                new Claim("UserTitle", "IT")
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var tempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            return controller;
        }

        [Fact]
        public async Task Edit_Get_LockedIfCompleted()
        {
            using var context = GetContext();
            var transfer = new EmployeeTransfer { FullName = "Test", Status = RequestStatus.Completed };
            context.EmployeeTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            var result = await controller.Edit(transfer.Id);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
        }

        [Fact]
        public async Task Edit_Post_LockedIfCompleted()
        {
            using var context = GetContext();
            var transfer = new EmployeeTransfer { FullName = "Test", Status = RequestStatus.Completed };
            context.EmployeeTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            var model = new EmployeeTransferEditPageViewModel
            {
                Id = transfer.Id,
                FullName = transfer.FullName,
                FromDepartment = transfer.FromDepartment,
                ToDepartment = transfer.ToDepartment
            };
            var result = await controller.Edit(transfer.Id, model, null, null);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
        }
    }
}

