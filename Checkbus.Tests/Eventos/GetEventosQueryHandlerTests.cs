using Checkbus.ApiService.Application.Eventos.Queries;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Eventos;

public class GetEventosQueryHandlerTests
{
    private sealed class FakeEventoRepository(IReadOnlyList<Evento> eventos) : IEventoRepository
    {
        public Task AddAsync(Evento evento, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventosQueryHandler.");

        public Task<Evento?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventosQueryHandler.");

        public Task<IReadOnlyList<Evento>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(eventos);
    }

    private sealed class FakeUbicacionRepository(IReadOnlyList<Ubicacion> ubicaciones) : IUbicacionRepository
    {
        public int GetByIdCallCount { get; private set; }

        public Task<Ubicacion?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventosQueryHandler.");

        public Task AddAsync(Ubicacion ubicacion, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventosQueryHandler.");

        public Task<Ubicacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            GetByIdCallCount++;
            return Task.FromResult(ubicaciones.FirstOrDefault(u => u.Id == id));
        }
    }

    private static Ubicacion CreateUbicacion(string nombre) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = nombre,
        Direccion = "Dirección de prueba",
        PlaceId = $"place-{Guid.NewGuid()}",
        Latitud = -31.0,
        Longitud = -64.0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Evento CreateEvento(Guid ubicacionId, string nombre) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = nombre,
        Tipo = EventoTipo.Concierto,
        Fecha = new DateTime(2026, 6, 1, 21, 0, 0, DateTimeKind.Utc),
        UbicacionId = ubicacionId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_ReturnsEventosWithResolvedUbicacion()
    {
        var ubicacion = CreateUbicacion("Movistar Arena");
        var evento = CreateEvento(ubicacion.Id, "Recital de rock");
        var handler = new GetEventosQueryHandler(
            new FakeEventoRepository([evento]), new FakeUbicacionRepository([ubicacion]));

        var result = await handler.Handle(new GetEventosQuery(), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(evento.Id, dto.Id);
        Assert.Equal("Recital de rock", dto.Nombre);
        Assert.Equal("Movistar Arena", dto.Ubicacion.Nombre);
    }

    [Fact]
    public async Task Handle_MultipleEventosSharingUbicacion_ResolvesEachUbicacionOnlyOnce()
    {
        var ubicacion = CreateUbicacion("Estadio Monumental");
        var eventoA = CreateEvento(ubicacion.Id, "Partido A");
        var eventoB = CreateEvento(ubicacion.Id, "Partido B");
        var ubicacionRepository = new FakeUbicacionRepository([ubicacion]);
        var handler = new GetEventosQueryHandler(
            new FakeEventoRepository([eventoA, eventoB]), ubicacionRepository);

        var result = await handler.Handle(new GetEventosQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal("Estadio Monumental", dto.Ubicacion.Nombre));
        Assert.Equal(1, ubicacionRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_NoEventos_ReturnsEmptyList()
    {
        var handler = new GetEventosQueryHandler(
            new FakeEventoRepository([]), new FakeUbicacionRepository([]));

        var result = await handler.Handle(new GetEventosQuery(), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }
}
