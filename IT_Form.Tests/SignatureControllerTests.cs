using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using IT_Form.ViewModels;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    public class SignatureControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task Acknowledge_Get_FailsOnHmacMismatch_Adversarial()
        {
            using var context = CreateContext();
            var req = new OnboardingRequest { 
                Id = 1, 
                AccessToken = Guid.NewGuid(), 
                Status = RequestStatus.PendingEmployeeSign,
                EmployeeSignedAt = DateTime.UtcNow,
                EmployeeSignature = "data:image/png;base64,TAMPERED",
                EmployeeSignatureHmac = "FORGED_HMAC" // Mismatch
            };
            context.OnboardingRequests.Add(req);
            await context.SaveChangesAsync();

            var controller = GetController<SignatureController>(context);
            
            // Call GET Acknowledge which performs integrity check
            var result = await controller.Acknowledge(req.AccessToken, "Onboarding");

            Assert.IsType<BadRequestObjectResult>(result); 
        }

        [Fact]
        public async Task SignTransfer_Forbidden_ForWrongRole_Adversarial()
        {
            using var context = CreateContext();
            var transfer = new EmployeeTransfer { Id = 1, Status = RequestStatus.PendingHODApproval };
            context.EmployeeTransfers.Add(transfer);
            await context.SaveChangesAsync();

            // IT Admin trying to sign as HR (Role bypass attempt)
            var controller = GetController<SignatureController>(context, role: "IT");
            var result = await controller.SignTransfer(transfer.Id, "HR", "data:image/png;base64,SIG");

            // Should return ForbidResult for unauthorized role
            Assert.IsType<ForbidResult>(result);
            // Verify success is false in JSON
        }

        [Fact]
        public async Task Acknowledge_Post_MissingSignature_RedisplaysViewWithValidationError()
        {
            using var context = CreateContext();
            var req = new OnboardingRequest
            {
                Id = 1,
                AccessToken = Guid.NewGuid(),
                FullName = "Test Employee",
                Department = "IT",
                Branch = "HQ",
                Status = RequestStatus.PendingEmployeeSign
            };
            context.OnboardingRequests.Add(req);
            await context.SaveChangesAsync();

            var controller = GetController<SignatureController>(context);

            var result = await controller.Acknowledge(req.AccessToken, "Onboarding", string.Empty);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("SignatureData"));
            Assert.Contains(
                controller.ModelState["SignatureData"]!.Errors,
                error => error.ErrorMessage == "Please provide your signature.");
            Assert.Equal("Acknowledge", viewResult.ViewName);
        }

        [Fact]
        public async Task SignTransfer_Post_MissingSignature_RedisplaysViewWithValidationError()
        {
            using var context = CreateContext();
            context.Users.Add(new ApplicationUser
            {
                Id = "u1",
                UserName = "testuser",
                FullName = "Test HR",
                Signature = "data:image/png;base64,DEFAULT"
            });

            var transfer = new EmployeeTransfer
            {
                Id = 1,
                Status = RequestStatus.PendingHRApproval
            };
            context.EmployeeTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var controller = GetController<SignatureController>(context, role: "HR");

            var result = await controller.SignTransfer(transfer.Id, "HR", string.Empty);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TransferSignaturePageViewModel>(viewResult.Model);
            Assert.Equal(transfer.Id, model.Id);
            Assert.True(controller.ModelState.ContainsKey("SignatureData"));
            Assert.Contains(
                controller.ModelState["SignatureData"]!.Errors,
                error => error.ErrorMessage == "Signature is required.");
        }
    }
}
