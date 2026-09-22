using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Authentication.Authorization;
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
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public DocumentType DocumentType { get; set; } = DocumentType.DNI;
        public required string DocumentNumber { get; set; }
        public Guid RoleId { get; set; }
        public required Role Role { get; set; }
        public Guid OrganizationId { get; set; }
        public required Organization Organization { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
