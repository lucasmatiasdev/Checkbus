using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using MediatR;
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (UserNotFoundException)
            {
                return Unauthorized();
            }
            catch (InvalidCredentialsException)
            {
                return Unauthorized();
            }
            catch (UserInactiveException)
            {
                return Unauthorized();
            }
        }
    }
}
