using Checkbus.ApiService.Domain.Entities.Tenancy;
using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Interfaces
{
    public interface ITenantEntity
    {
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }
    }
}
