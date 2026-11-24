using Checkbus.ApiService.Application.Tenancy.Commands;
using Checkbus.ApiService.Application.Tenancy.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class OrganizationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrganizationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "CheckbusSoporte")]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterOrganizationCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [Authorize(Roles = "CheckbusSoporte")]
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetOrganizationsQuery(), cancellationToken);
            return Ok(result);
        }
    }
}
