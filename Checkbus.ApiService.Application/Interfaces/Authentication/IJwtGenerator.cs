using Checkbus.ApiService.Domain.Entities.Authentication;
using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Application.Interfaces.Authentication
{
    public interface IJwtGenerator
    {
        string GenerateToken(User user);
    }
}
