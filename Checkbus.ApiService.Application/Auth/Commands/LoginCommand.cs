using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public class LoginCommand
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
