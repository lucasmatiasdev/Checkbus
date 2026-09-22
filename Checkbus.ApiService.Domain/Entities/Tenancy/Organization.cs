using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Entities.Tenancy
{
    public class Organization
    {
        public Guid Id { get; set; }
        public required string CUIT { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public required string LogoUrl { get; set; }
        public bool IsActive { get; set; }
    }
}
