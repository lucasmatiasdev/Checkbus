using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Tenancy.Commands
{

    public class RegisterOrganizationCommand : IRequest<RegisterOrganizationCommandResult>
    {
        public required string OrganizationName { get; set; }
        public required string CUIT { get; set; }
        public required string AdminName { get; set; }
        public required string AdminSurname { get; set; }
        public DocumentType AdminDocumentType { get; set; } = DocumentType.DNI;
        public required string AdminDocumentNumber { get; set; }

    }
}
