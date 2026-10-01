using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me([FromServices] ICurrentUserService currentUser)
        {
            var response = new CurrentUserResponse(
                currentUser.UserId,
                currentUser.OrganizationId,
                currentUser.Role,
                currentUser.Email);
            return Ok(response);
        }
    }
}
