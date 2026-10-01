using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public class LoginCommandResult
    {
        public required string Token { get; set; }
        public required Guid UserId { get; set; }
        public required Guid OrganizationId { get; set; }
        public required string Role { get; set; }
        public required string Email { get; set; }
    }
}
