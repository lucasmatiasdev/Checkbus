using MediatR;

namespace Checkbus.ApiService.Application.Viajes.Queries
{
    public class GetViajeQuery : IRequest<ViajeDetailDto>
    {
        public required Guid Id { get; set; }
    }
}
