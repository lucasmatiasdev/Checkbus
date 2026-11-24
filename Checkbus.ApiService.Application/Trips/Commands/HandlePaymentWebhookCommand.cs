using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class HandlePaymentWebhookCommand : IRequest
    {
        public required string ExternalEventId { get; set; }
        public required string ExternalOrderId { get; set; }
    }
}
