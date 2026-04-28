// Project comment: SubmitOffboardingStep1Command.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Models;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Offboarding.Step1
{
    /// <summary>
    /// Carries data needed to execute the SubmitOffboardingStep1 command.
    /// </summary>
    public sealed record SubmitOffboardingStep1Command(
        int Id,
        string SaveAction,
        OffboardingRequest Request,
        IFormFile? HardwareDamageImage,
        IFormFile? HardwareReturnImage,
        List<int>? ReturnedAssetIds) : IRequest<ProcessResult<OffboardingStep1PageViewModel>>;
}
