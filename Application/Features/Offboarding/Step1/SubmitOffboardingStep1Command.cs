using IT_Form.Application.Common.Models;
using IT_Form.Models;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Offboarding.Step1
{
    public sealed record SubmitOffboardingStep1Command(
        int Id,
        string SaveAction,
        OffboardingRequest Request,
        IFormFile? HardwareDamageImage,
        IFormFile? HardwareReturnImage,
        List<int>? ReturnedAssetIds) : IRequest<ProcessResult<OffboardingStep1PageViewModel>>;
}
