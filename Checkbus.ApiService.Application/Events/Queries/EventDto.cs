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

        // Deliberately NOT the raw ImageKey — same reasoning as VehicleDocumentResponse never
        // exposing FileKey: it's an opaque storage key, not meant to leave the server. A consumer
        // that wants the image fetches GET api/Events/{id}/image directly when HasImage is true.
        public bool HasImage { get; set; }

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
