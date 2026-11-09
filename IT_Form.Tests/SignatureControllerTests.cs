// Project comment: SignatureControllerTests.cs is part of the IT Form application source code.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IT_Form.Controllers;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the SignatureControllerTests class used by the IT Form application.
    /// </summary>
    public class SignatureControllerTests : ControllerTestBase
    {
        [Fact]
        /// <summary>
        /// Runs the Acknowledge_Get_FailsOnHmacMismatch_Adversarial operation.
        /// </summary>
        public async Task Acknowledge_Get_FailsOnHmacMismatch_Adversarial()
        {
            using var context = CreateContext();
            var req = new OnboardingRequest { 
                Id = 1, 
                AccessToken = Guid.NewGuid(), 
                Status = RequestStatus.PendingEmployeeSign,
                RequesterSignerName = "HR User",
                RequesterSignedAt = DateTime.UtcNow.AddHours(-2),
                RequesterSignature = "data:image/png;base64,REQ",
                RequesterSignatureHmac = "HMAC",
                ITAdminSignerName = "IT User",
                ITAdminSignedAt = DateTime.UtcNow.AddHours(-1),
                ITAdminSignature = "data:image/png;base64,IT",
                ITAdminSignatureHmac = "HMAC",
                EmployeeSignedAt = DateTime.UtcNow,
                EmployeeSignature = "data:image/png;base64,TAMPERED",
                EmployeeSignatureHmac = "FORGED_HMAC" // Mismatch
            };
            context.OnboardingRequests.Add(req);
            await context.SaveChangesAsync();
            MockIntegrityService
                .Setup(service => service.Verify(
                    "OnboardingRequest",
                    "1",
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    "data:image/png;base64,TAMPERED",
                    "FORGED_HMAC"))
                .Returns(false);

            var controller = GetController<SignatureController>(context);
            
            // Call GET Acknowledge which performs integrity check
            var result = await controller.Acknowledge(req.AccessToken, "Onboarding");

            Assert.IsType<BadRequestObjectResult>(result); 
        }

        [Fact]
        /// <summary>
        /// Runs the Acknowledge_Post_MissingSignature_RedisplaysViewWithValidationError operation.
        /// </summary>
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
                Status = RequestStatus.PendingEmployeeSign,
                RequesterSignerName = "HR User",
                RequesterSignedAt = DateTime.UtcNow.AddHours(-2),
                RequesterSignature = "data:image/png;base64,REQ",
                RequesterSignatureHmac = "HMAC",
                ITAdminSignerName = "IT User",
                ITAdminSignedAt = DateTime.UtcNow.AddHours(-1),
                ITAdminSignature = "data:image/png;base64,IT",
                ITAdminSignatureHmac = "HMAC",
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
        /// <summary>
        /// Runs the Acknowledge_Post_MissingPdpaConsent_RedisplaysViewWithValidationError operation.
        /// </summary>
        public async Task Acknowledge_Post_MissingPdpaConsent_RedisplaysViewWithValidationError()
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

            var result = await controller.Acknowledge(
                req.AccessToken,
                "Onboarding",
                "data:image/png;base64,iVBORw0KGgo=",
                pdpaConsent: false);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("PdpaConsent"));
            Assert.Contains(
                controller.ModelState["PdpaConsent"]!.Errors,
                error => error.ErrorMessage == "Please confirm the PDPA privacy notice before submitting.");
            Assert.Equal("Acknowledge", viewResult.ViewName);
        }
    }
}
