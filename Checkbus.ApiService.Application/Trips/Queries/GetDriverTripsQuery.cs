using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // "Mis viajes" (odd/tasks/reservas-publicas.md Fase 3 / A2) — scoped exclusively to the
    // caller's own driver identity (Trip.DriverId == the authenticated Chofer's own User.Id,
    // confirmed by reading CreateTripCommandHandler: there is no separate Driver entity,
    // DriverId IS a User.Id for a user with Role.Chofer). No request fields, same convention as
    // GetTripsQuery.
    public class GetDriverTripsQuery : IRequest<IReadOnlyList<TripDto>>
    {
    }
}
