using MediatR;

namespace Checkbus.ApiService.Application.Payments.Queries
{

    public class GetMercadoPagoConnectionStatusQuery : IRequest<MercadoPagoConnectionStatusDto>
    {
    }
}
