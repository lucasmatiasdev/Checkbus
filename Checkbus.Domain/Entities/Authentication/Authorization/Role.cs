using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkbus.Domain.Entities.Authentication.Authorization
{
    public class Role
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }
}
