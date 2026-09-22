using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Application.Common;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        ICommandHandler<LoginCommand, LoginCommandResult> _handler;
        public AuthController(ICommandHandler<LoginCommand, LoginCommandResult> handler)
        {
            _handler = handler;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            try
            {
                var result = await _handler.HandleAsync(command);
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
