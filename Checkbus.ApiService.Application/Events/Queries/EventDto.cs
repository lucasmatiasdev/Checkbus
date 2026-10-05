using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Events.Queries
{
    public class EventDto
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public EventType Type { get; set; }
        public DateTime Date { get; set; }
        public required LocationDto Location { get; set; }

        // Nested rather than a flat set of Location* fields — same shape choice as
        // VehicleDiagnosticDto/ComponentDiagnosticDto's parent/child DTO nesting.
        public class LocationDto
        {
            public required string Name { get; set; }
            public required string Address { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}
