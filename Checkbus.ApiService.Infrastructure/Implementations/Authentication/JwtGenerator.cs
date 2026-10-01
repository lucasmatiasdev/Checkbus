using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Checkbus.ApiService.Infrastructure.Implementations.Authentication
{
    public class JwtGenerator : IJwtGenerator
    {
        JwtOptions _options;
        public JwtGenerator(JwtOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SigningKey))
                throw new ArgumentException("JWT SigningKey is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.Issuer))
                throw new ArgumentException("JWT Issuer is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.Audience))
                throw new ArgumentException("JWT Audience is required.", nameof(options));
            _options = options;
        }
        public string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim("OrganizationId", user.OrganizationId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
                SigningCredentials = credentials
            };
            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }
}
