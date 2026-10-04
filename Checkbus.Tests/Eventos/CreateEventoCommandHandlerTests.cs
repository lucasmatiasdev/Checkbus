using Checkbus.ApiService.Application.Eventos.Commands;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Eventos;

public class CreateEventoCommandHandlerTests
{
    private sealed class FakeUbicacionRepository(Ubicacion? existingByPlaceId) : IUbicacionRepository
    {
        public List<Ubicacion> AddedUbicaciones { get; } = [];

        public Task<Ubicacion?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
            => Task.FromResult(existingByPlaceId is not null && existingByPlaceId.PlaceId == placeId
                ? existingByPlaceId
                : null);

        public Task AddAsync(Ubicacion ubicacion, CancellationToken cancellationToken)
        {
            AddedUbicaciones.Add(ubicacion);
            return Task.CompletedTask;
        }

        public Task<Ubicacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateEventoCommandHandler.");
    }

    private sealed class FakeEventoRepository : IEventoRepository
    {
        public List<Evento> AddedEventos { get; } = [];

        public Task AddAsync(Evento evento, CancellationToken cancellationToken)
        {
            AddedEventos.Add(evento);
            return Task.CompletedTask;
        }

        public Task<Evento?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateEventoCommandHandler.");

        public Task<IReadOnlyList<Evento>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateEventoCommandHandler.");
    }

    private static CreateEventoCommand CreateCommand() => new()
    {
        Nombre = "Final Copa Argentina",
        Tipo = EventoTipo.Partido,
        Fecha = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
        UbicacionNombre = "Estadio Mario Alberto Kempes",
        Direccion = "Av. Cardeñosa, Córdoba",
        PlaceId = "place-kempes-1",
        Latitud = -31.3333,
        Longitud = -64.2333
    };

    [Fact]
    public async Task Handle_NewPlaceId_CreatesUbicacionAndEvento()
    {
        var ubicacionRepository = new FakeUbicacionRepository(existingByPlaceId: null);
        var eventoRepository = new FakeEventoRepository();
        var handler = new CreateEventoCommandHandler(ubicacionRepository, eventoRepository);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        var createdUbicacion = Assert.Single(ubicacionRepository.AddedUbicaciones);
        Assert.Equal("place-kempes-1", createdUbicacion.PlaceId);
        Assert.Equal("Estadio Mario Alberto Kempes", createdUbicacion.Nombre);
        Assert.Equal("Av. Cardeñosa, Córdoba", createdUbicacion.Direccion);
        Assert.Equal(-31.3333, createdUbicacion.Latitud);
        Assert.Equal(-64.2333, createdUbicacion.Longitud);

        var createdEvento = Assert.Single(eventoRepository.AddedEventos);
        Assert.Equal(createdUbicacion.Id, createdEvento.UbicacionId);
        Assert.Equal("Final Copa Argentina", createdEvento.Nombre);
        Assert.Equal(EventoTipo.Partido, createdEvento.Tipo);

        Assert.Equal(createdEvento.Id, result.Id);
        Assert.Equal("Final Copa Argentina", result.Nombre);
        Assert.Equal(EventoTipo.Partido, result.Tipo);
        Assert.Equal("Estadio Mario Alberto Kempes", result.Ubicacion.Nombre);
        Assert.Equal("Av. Cardeñosa, Córdoba", result.Ubicacion.Direccion);
    }

    [Fact]
    public async Task Handle_ExistingPlaceId_ReusesUbicacion_DoesNotCreateNewRow()
    {
        var existing = new Ubicacion
        {
            Id = Guid.NewGuid(),
            Nombre = "Estadio Mario Alberto Kempes",
            Direccion = "Av. Cardeñosa, Córdoba",
            PlaceId = "place-kempes-1",
            Latitud = -31.3333,
            Longitud = -64.2333,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var ubicacionRepository = new FakeUbicacionRepository(existing);
        var eventoRepository = new FakeEventoRepository();
        var handler = new CreateEventoCommandHandler(ubicacionRepository, eventoRepository);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Empty(ubicacionRepository.AddedUbicaciones);

        var createdEvento = Assert.Single(eventoRepository.AddedEventos);
        Assert.Equal(existing.Id, createdEvento.UbicacionId);
        Assert.Equal(existing.Nombre, result.Ubicacion.Nombre);
    }

    [Fact]
    public async Task Handle_ReturnsDtoMappedFromCreatedEventoAndUbicacion()
    {
        var ubicacionRepository = new FakeUbicacionRepository(existingByPlaceId: null);
        var eventoRepository = new FakeEventoRepository();
        var handler = new CreateEventoCommandHandler(ubicacionRepository, eventoRepository);
        var command = CreateCommand();

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(command.Nombre, result.Nombre);
        Assert.Equal(command.Tipo, result.Tipo);
        Assert.Equal(command.Fecha, result.Fecha);
        Assert.Equal(command.UbicacionNombre, result.Ubicacion.Nombre);
        Assert.Equal(command.Direccion, result.Ubicacion.Direccion);
        Assert.Equal(command.Latitud, result.Ubicacion.Latitud);
        Assert.Equal(command.Longitud, result.Ubicacion.Longitud);
    }
}
