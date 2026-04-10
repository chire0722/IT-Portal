using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Offboarding.ActionWrites;
using IT_Form.Application.Features.Offboarding.DeleteOffboardingRequest;
using IT_Form.Application.Features.Offboarding.Flows;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IT_Form.Controllers
{
    [Authorize]
    [Route("api/offboarding-requests")]
    [EnableRateLimiting("fixed")]
    public sealed class OffboardingApiController : AppApiControllerBase
    {
        private readonly ISender _sender;

        public OffboardingApiController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] string? searchString, [FromQuery] RequestStatus? status, [FromQuery] int page = 1)
            => HandleProcessResult(await _sender.Send(new GetOffboardingIndexApiQuery(searchString, status, page)));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetails(int id)
            => HandleProcessResult(await _sender.Send(new GetOffboardingDetailsApiQuery(id)));

        [HttpPost("{id:int}/sign/hr")]
        public async Task<IActionResult> SignHr(int id, [FromBody] SignatureBase64ApiRequest request)
            => HandleCommandResponse(await _sender.Send(new ProcessOffboardingHrSignatureCommand(id, request.SignatureBase64)));

        [HttpPost("{id:int}/sign/manager")]
        public async Task<IActionResult> SignManager(int id, [FromBody] SignatureBase64ApiRequest request)
            => HandleCommandResponse(await _sender.Send(new ProcessOffboardingManagerSignatureCommand(id, request.SignatureBase64)));

        [HttpPost("{id:int}/sign/it")]
        public async Task<IActionResult> SignIt(int id, [FromBody] SignatureBase64ApiRequest request)
            => HandleCommandResponse(await _sender.Send(new ProcessOffboardingItSignatureCommand(id, request.SignatureBase64)));

        [HttpPost("{id:int}/sign/employee")]
        public async Task<IActionResult> SignEmployee(int id, [FromBody] SignatureBase64ApiRequest request)
            => HandleCommandResponse(await _sender.Send(new ProcessOffboardingEmployeeManualSignatureCommand(id, request.SignatureBase64)));

        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectionApiRequest request)
            => HandleCommandResponse(await _sender.Send(new ProcessOffboardingRejectionCommand(id, request.Reason)));

        [HttpPost("{id:int}/regenerate-token")]
        public async Task<IActionResult> RegenerateToken(int id)
            => HandleCommandResponse(await _sender.Send(new ProcessOffboardingTokenRegenerationCommand(id)));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
            => HandleCommandResponse(await _sender.Send(new DeleteOffboardingRequestCommand(id)));
    }
}
