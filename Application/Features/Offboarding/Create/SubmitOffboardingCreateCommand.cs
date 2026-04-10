using IT_Form.Application.Common.Models;
using IT_Form.Models;
using IT_Form.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Offboarding.Create
{
    public sealed record SubmitOffboardingCreateCommand(
        OffboardingRequest Request,
        IFormFile? ResignationLetter) : IRequest<ProcessResult<OffboardingCreatePageViewModel>>;
}
