using Checkbus.ApiService.Application.Users.Commands;
using Checkbus.ApiService.Application.Users.Queries;
using Checkbus.ApiService.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = nameof(Role.Administrador))]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [Authorize(Roles = nameof(Role.Administrador))]
        [HttpGet]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUsersQuery(), cancellationToken);
            return Ok(result);
        }
    }
}
