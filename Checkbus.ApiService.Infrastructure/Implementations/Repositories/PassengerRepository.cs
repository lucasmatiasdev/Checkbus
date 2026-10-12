using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class PassengerRepository : IPassengerRepository
    {
        private readonly CheckbusDbContext _context;

        public PassengerRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Passengers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }
    }
}
