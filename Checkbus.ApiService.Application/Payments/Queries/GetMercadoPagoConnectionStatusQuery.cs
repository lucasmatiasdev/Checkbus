using MediatR;

namespace Checkbus.ApiService.Application.Payments.Queries
{
    // odd/tasks/pagos-mercadopago.md Fase I, I8 — backs the Payments settings page's own status
    // read. Deliberately ABSENT: OrganizationId — same anti-IDOR convention as every other
    // authenticated query/command in this codebase.
    public class GetMercadoPagoConnectionStatusQuery : IRequest<MercadoPagoConnectionStatusDto>
    {
    }
}
