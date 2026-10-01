using MediatR;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public class ChangePasswordCommand : IRequest
    {
        public required string NewPassword { get; set; }
        // Deliberately ABSENT: any user-identifying field. The target is always
        // ICurrentUserService.UserId (the validated JWT claim) — a request body
        // cannot target another user's password (anti-IDOR, D-9).
    }
}
