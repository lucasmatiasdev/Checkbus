using MediatR;

namespace Checkbus.ApiService.Application.Payments.Commands
{
    // Authenticated "start connection" action (odd/tasks/pagos-mercadopago.md Fase I, I7).
    // Deliberately ABSENT: OrganizationId — resolved exclusively from the authenticated caller's
    // token inside the handler, same anti-IDOR rationale GetVehiclesQuery/RegisterUserCommand
    // already establish.
    public class StartMercadoPagoConnectionCommand : IRequest<StartMercadoPagoConnectionResultDto>
    {
    }
}
