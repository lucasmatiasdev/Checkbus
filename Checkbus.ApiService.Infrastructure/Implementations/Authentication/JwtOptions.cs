using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Infrastructure.Implementations.Authentication
{
    public class JwtOptions
    {
        public required string SigningKey { get; set; }
        public int ExpirationMinutes { get; set; } = 60;
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
    }
}
