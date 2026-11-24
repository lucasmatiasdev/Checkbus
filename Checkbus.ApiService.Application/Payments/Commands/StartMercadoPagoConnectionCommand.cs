using MediatR;

namespace Checkbus.ApiService.Application.Payments.Commands
{

    public class StartMercadoPagoConnectionCommand : IRequest<StartMercadoPagoConnectionResultDto>
    {
    }
}
