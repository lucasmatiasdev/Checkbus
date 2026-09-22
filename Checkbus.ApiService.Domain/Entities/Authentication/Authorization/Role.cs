using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Entities.Authentication.Authorization
{
    public class Role
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
    }
}
