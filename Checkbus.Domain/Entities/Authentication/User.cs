using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;
using Checkbus.Domain.Enums;
using Checkbus.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkbus.Domain.Entities.Authentication
{
    public class User : ITenantEntity
    {
        public int Id { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required Organization Organization { get; set; }
        public int OrganizationId { get; set; }
        public required Profile Profile { get; set; }
        public int ProfileId { get; set; }
        public DocumentType DocumentType { get; set; } = DocumentType.DNI;
        public required string DocumentNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTimeOffset? LockedUntil { get; set; }
        public bool MustChangePassword { get; set; } = true;
    }
}
