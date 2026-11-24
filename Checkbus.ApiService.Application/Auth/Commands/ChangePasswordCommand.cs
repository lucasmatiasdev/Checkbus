using MediatR;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public class ChangePasswordCommand : IRequest
    {
        public required string NewPassword { get; set; }

    }
}
