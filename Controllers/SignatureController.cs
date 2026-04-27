using IT_Form.Application.Features.Signature;
using IT_Form.Application.Features.Signature.AcknowledgeEmployeeSignature;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace IT_Form.Controllers
{
    public class SignatureController : AppControllerBase
    {
        private readonly ISender _sender;

        public SignatureController(ISender sender) => _sender = sender;

        // GET: /Signature/Acknowledge?token=xxx&type=Onboarding
        public async Task<IActionResult> Acknowledge(Guid token, string type)
            => HandleAcknowledgePageResult(await _sender.Send(new GetSignatureAcknowledgePageQuery(token, type)));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Acknowledge(Guid token, string type, string signatureData, bool pdpaConsent = false)
        {
            return await ExecuteWithValidationResult(
                async () =>
                {
                    var result = await _sender.Send(new AcknowledgeEmployeeSignatureCommand(token, type, signatureData, pdpaConsent));
                    if (result.IsNotFound)
                    {
                        return NotFound();
                    }

                    if (result.IsForbidden)
                    {
                        return Forbid();
                    }

                    if (!result.IsSuccess)
                    {
                        return BadRequest(result.Message);
                    }

                    return View("Success");
                },
                async () => HandleAcknowledgePageResult(await _sender.Send(new GetSignatureAcknowledgePageQuery(token, type))));
        }

        // GET: /Signature/ViewPdf?token=xxx&type=Onboarding
        [HttpGet]
        public async Task<IActionResult> ViewPdf(Guid token, string type)
            => HandlePdfResult(await _sender.Send(new GetSignaturePdfQuery(token, type)));

        private IActionResult HandleAcknowledgePageResult(GetSignatureAcknowledgePageResult result)
        {
            return result.Status switch
            {
                GetSignatureAcknowledgePageStatus.Ready => View("Acknowledge", result.ViewModel),
                GetSignatureAcknowledgePageStatus.NotFound => NotFound(result.Message),
                GetSignatureAcknowledgePageStatus.Invalid => BadRequest(result.Message),
                GetSignatureAcknowledgePageStatus.IntegrityFailed => BadRequest(result.Message),
                _ => BadRequest()
            };
        }

        private IActionResult HandlePdfResult(GetSignaturePdfResult result)
        {
            return result.Status switch
            {
                GetSignaturePdfStatus.Ready when result.File != null => File(result.File.Content, result.File.ContentType, result.File.FileDownloadName),
                GetSignaturePdfStatus.NotFound => NotFound(),
                GetSignaturePdfStatus.Invalid => BadRequest(result.Message),
                GetSignaturePdfStatus.IntegrityFailed => BadRequest(result.Message),
                GetSignaturePdfStatus.Expired => new ObjectResult(result.Message)
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },
                _ => BadRequest()
            };
        }

    }
}
