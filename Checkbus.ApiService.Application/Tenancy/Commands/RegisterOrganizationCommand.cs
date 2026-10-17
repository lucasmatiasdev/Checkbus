using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Tenancy.Commands
{
    // Internal CheckbusSoporte tool (odd/tasks/alta-organizacion-soporte.md) — creates a brand
    // new Organization together with its first Administrador User in one step. Not public
    // self-service: the controller action gates the caller to CheckbusSoporte.
    public class RegisterOrganizationCommand : IRequest<RegisterOrganizationCommandResult>
    {
        public required string OrganizationName { get; set; }
        public required string CUIT { get; set; }
        public required string AdminName { get; set; }
        public required string AdminSurname { get; set; }
        public DocumentType AdminDocumentType { get; set; } = DocumentType.DNI;
        public required string AdminDocumentNumber { get; set; }

        // Deliberately ABSENT: any OrganizationId (there is none yet — this command creates it)
        // and any role field, for either the caller or the new admin. The caller's role is
        // enforced exclusively by [Authorize(Roles = "CheckbusSoporte")] on the controller
        // action, and the new admin's Role is always Role.Administrador, decided server-side by
        // the handler — same "server decides, client never supplies a sensitive field"
        // convention as CreateVehicleCommand (OrganizationId) and RegisterUserCommand
        // (Email/Password/OrganizationId).
    }
}
