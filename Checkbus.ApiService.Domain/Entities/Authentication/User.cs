using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Authentication
{
    public class User : ITenantEntity
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public DocumentType DocumentType { get; set; } = DocumentType.DNI;
        public required string DocumentNumber { get; set; }
        public required Role Role { get; set; }
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public bool IsActive { get; set; } = true;
        // No initializer: relies on the CLR default `false` (and the resulting
        // `boolean NOT NULL DEFAULT false` column), so every pre-existing or seeded
        // user defaults to false with zero CheckbusDbSeeder edits. Only
        // RegisterUserCommandHandler sets this true, explicitly.
        public bool MustChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
