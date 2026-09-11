using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkbus.Domain.Entities.Tenancy
{
    public class Organization
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string CUIT { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
