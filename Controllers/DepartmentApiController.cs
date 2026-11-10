// Project comment: DepartmentApiController.cs is part of the IT Form application source code.

using IT_Form.Application.Features.Departments.Writes.CreateDepartment;
using IT_Form.Application.Features.Departments.Writes.DeleteDepartment;
using IT_Form.Application.Features.Departments.Writes.UpdateDepartment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IT_Form.Controllers
{
    [ApiController]
    [Authorize(Policy = "RequireIT")]
    [Route("api/departments")]
    /// <summary>
    /// Handles web requests for the DepartmentApi area.
    /// </summary>
    public sealed class DepartmentApiController : AppApiControllerBase
    {
        private readonly IMediator _mediator;

        public DepartmentApiController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Creates data for the Create operation.
        /// </summary>
        public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand command, CancellationToken cancellationToken)
        {
            var id = await _mediator.Send(command, cancellationToken);
            return Created($"/api/departments/{id}", new { id });
        }

        [HttpPut("{id:int}")]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Updates data for the Update operation.
        /// </summary>
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Deletes data for the Delete operation.
        /// </summary>
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteDepartmentCommand(id), cancellationToken);
            return NoContent();
        }
    }
}
