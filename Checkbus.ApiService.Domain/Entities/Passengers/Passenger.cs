using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Passengers
{

    public class Passenger
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required DocumentType DocumentType { get; set; }
        public required string DocumentNumber { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
    }
}
