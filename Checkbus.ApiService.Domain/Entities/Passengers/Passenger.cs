using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Passengers
{
    // Deliberately NOT a reusable catalog: a brand-new row is created on every reservation,
    // even when DocumentNumber already exists from a previous reservation — do not add a
    // unique index on DocumentNumber, that dedup is an explicit non-goal (see
    // odd/tasks/reservas-publicas.md). Email/Phone are captured now but unused until Fase 2
    // (sending the confirmation by mail); no "send" code path exists yet. DocumentType reuses
    // the existing enum already used by User, rather than a new passenger-specific one.
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
