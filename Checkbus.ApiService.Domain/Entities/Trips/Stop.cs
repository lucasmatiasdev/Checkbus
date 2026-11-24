using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Trips
{

    public class Stop
    {
        public Guid Id { get; set; }
        public required Guid RouteId { get; set; }
        public required Guid LocationId { get; set; }
        public int Order { get; set; }
        public StopType Type { get; set; }

        public DateTime? DepartureTime { get; set; }

        public int StopDurationMinutes { get; set; }

        public bool IsBoardingPoint { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
