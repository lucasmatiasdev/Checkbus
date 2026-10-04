using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Trips
{
    // Shared catalog of real-world events — deliberately no OrganizationId, multiple
    // organizations can publish trips to the same event. FK-only relationship to
    // Location, no navigation property back.
    public class Event
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public EventType Type { get; set; }
        public DateTime Date { get; set; }
        public required Guid LocationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
