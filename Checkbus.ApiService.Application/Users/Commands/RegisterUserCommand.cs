using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Users.Commands
{
    public class RegisterUserCommand : IRequest<RegisterUserCommandResult>
    {
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public DocumentType DocumentType { get; set; } = DocumentType.DNI;
        public required string DocumentNumber { get; set; }
        public required Role Role { get; set; }

    }
}
