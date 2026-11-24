using MediatR;

namespace Checkbus.ApiService.Application.Payments.Commands
{

    public class HandleMercadoPagoOAuthCallbackCommand : IRequest<MercadoPagoOAuthCallbackResultDto>
    {
        public required string Code { get; set; }
        public required string State { get; set; }
    }
}
