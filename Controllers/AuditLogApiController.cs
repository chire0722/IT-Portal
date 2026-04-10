using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.AuditLogs.Flows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;

namespace IT_Form.Controllers
{
    [ApiController]
    [Authorize(Policy = "RequireHROrIT")]
    [Route("api/audit-logs")]
    [EnableRateLimiting("fixed")]
    public sealed class AuditLogApiController : AppApiControllerBase
    {
        private readonly ISender _sender;

        public AuditLogApiController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] string? entity,
            [FromQuery] string? auditAction,
            [FromQuery] string? search,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] bool? isSuccess,
            [FromQuery] int page = 1)
            => Ok(await _sender.Send(new GetAuditLogListApiQuery(entity, auditAction, search, startDate, endDate, isSuccess, page)));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetails(int id)
            => HandleProcessResult(await _sender.Send(new GetAuditLogDetailsApiQuery(id)));

        [HttpGet("{id:int}/chain")]
        public async Task<IActionResult> GetChain(int id, [FromQuery] bool failuresOnly = false)
            => HandleProcessResult(await _sender.Send(new GetAuditLogChainApiQuery(id, failuresOnly)));

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] string? entity,
            [FromQuery] string? auditAction,
            [FromQuery] string? search,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] bool? isSuccess)
            => Ok(await _sender.Send(new GetAuditLogSummaryApiQuery(entity, auditAction, search, startDate, endDate, isSuccess)));
    }
}
