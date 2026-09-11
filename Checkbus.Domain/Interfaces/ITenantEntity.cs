using Checkbus.Domain.Entities.Tenancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkbus.Domain.Interfaces
{
    public interface ITenantEntity
    {
        public Organization Organization { get; set; }
    }
}
