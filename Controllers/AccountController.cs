// Project comment: AccountController.cs is part of the IT Form application source code.

using System.Security.Claims;
using IT_Form.Application.Features.Account.ForgotPassword;
using IT_Form.Application.Features.Account.LoginAccount;
using IT_Form.Application.Features.Account;
using IT_Form.Application.Features.Account.Queries.GetLoginPageBranding;
using IT_Form.Application.Features.Account.Queries.GetProfilePage;
using IT_Form.Application.Features.Account.Queries.GetVerifyOtpPage;
using IT_Form.Application.Features.Account.ResetPassword;
using IT_Form.Application.Features.Account.VerifyOtp;
using IT_Form.Application.Features.Account.Writes.ForceChangePassword;
using IT_Form.Application.Features.Account.Writes.Logout;
using IT_Form.Application.Features.Account.Writes.ProfilePasswordChange;
using IT_Form.Application.Features.Account.Writes.ResendOtp;
using IT_Form.Application.Features.Account.Writes.UpdateProfile;
using IT_Form.Application.Features.Account.Writes.UpdateSignature;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IT_Form.Controllers
{
    /// <summary>
    /// Handles web requests for the Account area.
    /// </summary>
    public class AccountController : AppControllerBase
    {
        private readonly ISender _sender;

        public AccountController(ISender sender)
        {
            _sender = sender;
        }

        // GET: /Account/Login
        [HttpGet]
        /// <summary>
        /// Runs the Login operation.
        /// </summary>
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(await BuildLoginPageAsync(returnUrl: returnUrl));
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-login")]
        /// <summary>
        /// Runs the Login operation.
        /// </summary>
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(await BuildLoginPageAsync(model, returnUrl: returnUrl));
            }

            return await ExecuteWithValidationView(
                async () =>
                {
                    var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var result = await _sender.Send(new LoginAccountCommand(
                        model.Username,
                        model.Password,
                        model.RememberMe,
                        clientIp));

                    if (result.IsAuthenticated)
                    {
                        if (result.MustChangePassword)
                        {
                            return RedirectToAction(nameof(ForceChangePassword));
                        }

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }

                        return RedirectToAction("Index", "Home");
                    }

                    if (result.RequiresOtp && !string.IsNullOrWhiteSpace(result.OtpEmail))
                    {
                        TempData["SuccessMessage"] = result.Message;
                        return RedirectToAction(nameof(VerifyOtp), new { email = result.OtpEmail, purpose = OtpPurposes.PrivilegedLogin });
                    }

                    var pageModel = await BuildLoginPageAsync(
                        model,
                        result.LockoutEndUtc?.ToString("o"),
                        returnUrl);

                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(pageModel);
                },
                () => BuildLoginPageAsync(model, returnUrl: returnUrl));
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the Logout operation.
        /// </summary>
        public async Task<IActionResult> Logout()
        {
            await _sender.Send(new LogoutCommand());
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        /// <summary>
        /// Runs the ForgotPassword operation.
        /// </summary>
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-recovery")]
        /// <summary>
        /// Runs the ForgotPassword operation.
        /// </summary>
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            return await ExecuteWithValidationView(
                async () =>
                {
                    var requestIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var result = await _sender.Send(new ForgotPasswordCommand(model.Username, requestIp));
                    if (result.OtpSent)
                    {
                        return RedirectToAction(nameof(VerifyOtp), new { email = result.Email });
                    }

                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(model);
                },
                () => Task.FromResult(model));
        }

        // GET: /Account/VerifyOtp
        [HttpGet]
        [AllowAnonymous]
        /// <summary>
        /// Runs the VerifyOtp operation.
        /// </summary>
        public async Task<IActionResult> VerifyOtp(string email, string? purpose = null)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(await BuildVerifyOtpPageAsync(email, purpose: purpose));
        }

        // POST: /Account/VerifyOtp
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-otp")]
        /// <summary>
        /// Runs the VerifyOtp operation.
        /// </summary>
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(await BuildVerifyOtpPageAsync(model.Email, model.Code));
            }

            return await ExecuteWithValidationView(
                async () =>
                {
                    var result = await _sender.Send(new VerifyOtpCommand(model.Email, model.Code, model.Purpose));
                    if (result.IsVerified)
                    {
                        if (result.IsLoginVerified)
                        {
                            return result.RequiresPasswordSetup
                                ? RedirectToAction(nameof(ForceChangePassword))
                                : RedirectToAction("Index", "Home");
                        }

                        return RedirectToAction(nameof(ResetPassword), new { email = result.Email ?? model.Email, token = result.Token });
                    }

                    if (result.ShouldRedirectToLogin)
                    {
                        return RedirectToAction(nameof(Login));
                    }

                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(await BuildVerifyOtpPageAsync(model.Email, model.Code, model.Purpose));
                },
                () => BuildVerifyOtpPageAsync(model.Email, model.Code, model.Purpose));
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        /// <summary>
        /// Runs the ResetPassword operation.
        /// </summary>
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-recovery")]
        /// <summary>
        /// Runs the ResetPassword operation.
        /// </summary>
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            return await ExecuteWithValidationView(
                async () =>
                {
                    var mediatorResult = await _sender.Send(new ResetPasswordCommand(model.Email, model.Token, model.NewPassword));
                    if (mediatorResult.ShouldRedirectToLogin)
                    {
                        return RedirectToAction(nameof(Login));
                    }

                    if (mediatorResult.Succeeded)
                    {
                        TempData["SuccessMessage"] = "Your password has been reset successfully. You can now log in.";
                        return RedirectToAction(nameof(Login));
                    }

                    foreach (var error in mediatorResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }

                    return View(model);
                },
                () => Task.FromResult(model));
        }

        // POST: /Account/ResendOtp
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-recovery")]
        /// <summary>
        /// Runs the ResendOtp operation.
        /// </summary>
        public async Task<IActionResult> ResendOtp(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Login));
            }

            return await ExecuteWithValidationRedirect(
                async () =>
                {
                    var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var purpose = Request.Form["purpose"].FirstOrDefault() ?? OtpPurposes.PasswordReset;
                    var result = await _sender.Send(new ResendOtpCommand(email, clientIp, purpose));
                    // Keep the browser response identical whether an account exists,
                    // is inactive, or is still inside the resend cooldown.
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(VerifyOtp), new { email = result.Email, purpose });
                },
                nameof(VerifyOtp),
                new { email });
        }

        // GET: /Account/Profile
        [HttpGet]
        [Authorize]
        /// <summary>
        /// Runs the Profile operation.
        /// </summary>
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction(nameof(Login));
            }

            return HandleProcessResult(await _sender.Send(new GetProfilePageQuery(userId)));
        }

        // POST: /Account/UpdateSignature
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Updates data for the UpdateSignature operation.
        /// </summary>
        public async Task<IActionResult> UpdateSignature(UpdateSignatureInputModel model)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var userId = GetCurrentUserId();
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return RedirectToAction(nameof(Login));
                    }

                    await _sender.Send(model.Adapt<UpdateSignatureCommand>() with { UserId = userId });
                    TempData["SuccessMessage"] = "Profile signature saved successfully!";
                    return RedirectToAction(nameof(Profile));
                },
                nameof(Profile),
                new { });

        // POST: /Account/UpdateProfile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Updates data for the UpdateProfile operation.
        /// </summary>
        public async Task<IActionResult> UpdateProfile(UpdateProfileInputModel model)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var userId = GetCurrentUserId();
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return RedirectToAction(nameof(Login));
                    }

                    await _sender.Send(model.Adapt<UpdateProfileCommand>() with { UserId = userId });
                    TempData["SuccessMessage"] = "Profile details saved successfully!";
                    return RedirectToAction(nameof(Profile));
                },
                nameof(Profile),
                new { });

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-recovery")]
        public async Task<IActionResult> BeginProfilePasswordChange(ProfilePasswordChangeInputModel model)
            => await ExecuteWithValidationRedirect(
                async () =>
                {
                    var userId = GetCurrentUserId();
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return RedirectToAction(nameof(Login));
                    }

                    var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var result = await _sender.Send(new BeginProfilePasswordChangeCommand(
                        userId,
                        model.CurrentPassword,
                        model.NewPassword,
                        model.ConfirmPassword,
                        clientIp));
                    TempData["SuccessMessage"] = "Verification code sent to your email.";
                    return RedirectToAction(nameof(ConfirmProfilePasswordChange), new { requestId = result.RequestId });
                },
                nameof(Profile),
                new { });

        [HttpGet]
        [Authorize]
        public IActionResult ConfirmProfilePasswordChange(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                TempData["ErrorMessage"] = "Password-change request is missing. Please start again.";
                return RedirectToAction(nameof(Profile));
            }

            return View(new ConfirmProfilePasswordChangeInputModel { RequestId = requestId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-otp")]
        public async Task<IActionResult> ConfirmProfilePasswordChange(ConfirmProfilePasswordChangeInputModel model)
        {
            return await ExecuteWithValidationView(
                async () =>
                {
                    var userId = GetCurrentUserId();
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return RedirectToAction(nameof(Login));
                    }

                    await _sender.Send(new ConfirmProfilePasswordChangeCommand(userId, model.RequestId, model.Code));
                    TempData["SuccessMessage"] = "Password changed successfully.";
                    return RedirectToAction(nameof(Profile));
                },
                () => Task.FromResult(model));
        }

        // GET: /Account/ForceChangePassword
        [HttpGet]
        [Authorize]
        /// <summary>
        /// Runs the ForceChangePassword operation.
        /// </summary>
        public IActionResult ForceChangePassword()
        {
            return View(new ForceChangePasswordPageViewModel());
        }

        // POST: /Account/ForceChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the ForceChangePassword operation.
        /// </summary>
        public async Task<IActionResult> ForceChangePassword(ForceChangePasswordPageViewModel model)
        {
            ModelState.Remove(nameof(model.CurrentPassword));

            return await ExecuteWithValidationView(
                async () =>
                {
                    var userId = GetCurrentUserId();
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return RedirectToAction(nameof(Login));
                    }

                    await _sender.Send(model.Adapt<ForceChangePasswordCommand>() with { UserId = userId });
                    TempData["SuccessMessage"] = "Password changed successfully. Welcome!";
                    return RedirectToAction("Index", "Home");
                },
                () => Task.FromResult(model));
        }

        [HttpGet]
        /// <summary>
        /// Runs the AccessDenied operation.
        /// </summary>
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Gets the data required by the GetCurrentUserId operation.
        /// </summary>
        private string? GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// Builds the value returned by the BuildLoginPageAsync operation.
        /// </summary>
        private async Task<LoginPageBrandingViewModel> BuildLoginPageAsync(
            LoginViewModel? form = null,
            string? lockoutEndTimeIso = null,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return await _sender.Send(new GetLoginPageBrandingQuery(form, lockoutEndTimeIso));
        }

        /// <summary>
        /// Builds the value returned by the BuildVerifyOtpPageAsync operation.
        /// </summary>
        private Task<VerifyOtpViewModel> BuildVerifyOtpPageAsync(string email, string? code = null, string? purpose = null)
            => _sender.Send(new GetVerifyOtpPageQuery(email, code, purpose ?? OtpPurposes.PasswordReset));
    }
}
