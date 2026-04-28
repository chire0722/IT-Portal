// Project comment: OnboardingApiController.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.ActionWrites;
using IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest;
using IT_Form.Application.Features.Onboarding.Flows;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IT_Form.Controllers
{
    [Authorize]
    [Route("api/onboarding-requests")]
    [EnableRateLimiting("fixed")]
    /// <summary>
    /// Handles web requests for the OnboardingApi area.
    /// </summary>
    public sealed class OnboardingApiController : AppApiControllerBase
    {
        private readonly ISender _sender;

        public OnboardingApiController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        /// <summary>
        /// Gets the data required by the GetList operation.
        /// </summary>
        public async Task<IActionResult> GetList([FromQuery] string? searchString, [FromQuery] RequestStatus? status, [FromQuery] int page = 1)
            => Ok(await _sender.Send(new GetOnboardingIndexApiQuery(searchString, status, page)));

        [HttpGet("{id:int}")]
        /// <summary>
        /// Gets the data required by the GetDetails operation.
        /// </summary>
        public async Task<IActionResult> GetDetails(int id)
        {
            return HandleProcessResult(await _sender.Send(new GetOnboardingDetailsApiQuery(id)));
        }

        [HttpPost("{id:int}/sign")]
        /// <summary>
        /// Runs the Sign operation.
        /// </summary>
        public async Task<IActionResult> Sign(int id, [FromBody] OnboardingSignatureApiRequest request)
            => HandleCommandResponse(await _sender.Send(new ProcessOnboardingSignatureCommand(id, request.SignatureType, request.SignatureData)));

        [HttpPost("{id:int}/reject")]
        /// <summary>
        /// Runs the Reject operation.
        /// </summary>
        public async Task<IActionResult> Reject(int id, [FromBody] RejectionApiRequest request)
            => HandleCommandResponse(await _sender.Send(new ProcessOnboardingRejectionCommand(id, request.Reason)));

        [HttpPost("{id:int}/regenerate-token")]
        /// <summary>
        /// Runs the RegenerateToken operation.
        /// </summary>
        public async Task<IActionResult> RegenerateToken(int id)
            => HandleCommandResponse(await _sender.Send(new ProcessOnboardingTokenRegenerationCommand(id)));

        [HttpDelete("{id:int}")]
        /// <summary>
        /// Deletes data for the Delete operation.
        /// </summary>
        public async Task<IActionResult> Delete(int id)
            => HandleProcessResult(await _sender.Send(new DeleteOnboardingRequestCommand(id, false)));
    }
}
