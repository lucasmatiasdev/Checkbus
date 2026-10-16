using System.Text;
using ClosedXML.Excel;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Reports;

namespace Checkbus.Tests.Reports;

public class ReportExportServiceTests
{
    private static TripDto CreateTripDto(TripStatus status = TripStatus.Programado) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 3, 10, 16, 0, 0, DateTimeKind.Utc),
        Capacity = 45,
        AvailableSeats = 40,
        Price = 1000m,
        Status = status,
        VehiclePatent = "AB123CD",
        VehicleBrand = "Mercedes-Benz",
        VehicleModel = "O500",
        DriverName = "Juan",
        DriverSurname = "Perez",
        EventName = "Final Copa Argentina"
    };

    private static TripsReportDto CreateReport(params TripDto[] trips) => new()
    {
        Trips = trips,
        Statistics = new TripsReportStatisticsDto
        {
            TotalTrips = trips.Length,
            TripsByStatus = trips.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count()),
            TotalPassengers = trips.Sum(t => t.Capacity - t.AvailableSeats),
            EstimatedRevenue = trips.Sum(t => t.Price * (t.Capacity - t.AvailableSeats))
        }
    };

    private static ReportExportService CreateSut() => new();

    [Fact]
    public void GenerateExcel_WithTrips_CreatesTripsSheetWithHeaderAndOneRowPerTrip()
    {
        var report = CreateReport(CreateTripDto(), CreateTripDto(TripStatus.Completado));
        var sut = CreateSut();

        var bytes = sut.GenerateExcel(report);

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet("Viajes");
        var lastRow = sheet.LastRowUsed()!.RowNumber();

        Assert.Equal("Fecha salida", sheet.Cell(1, 1).GetString());
        Assert.Equal(3, lastRow); // header + 2 trips
        Assert.Equal(report.Trips[0].VehiclePatent, sheet.Cell(2, 4).GetString());
    }

    [Fact]
    public void GenerateExcel_WithTrips_CreatesStatisticsSheetWithAggregates()
    {
        var report = CreateReport(CreateTripDto(), CreateTripDto(TripStatus.Completado));
        var sut = CreateSut();

        var bytes = sut.GenerateExcel(report);

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet("Estadisticas");

        var text = sheet.Cells().Select(c => c.GetString()).ToList();
        Assert.Contains("Total de viajes", text);
        Assert.Contains("2", text);
        Assert.Contains(report.Statistics.TotalPassengers.ToString(), text);
    }

    [Fact]
    public void GenerateExcel_NoTrips_HeaderOnlyNoException()
    {
        var report = CreateReport();
        var sut = CreateSut();

        var bytes = sut.GenerateExcel(report);

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet("Viajes");
        Assert.Equal(1, sheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public void GenerateCsv_WithTrips_HasHeaderAndOneLinePerTrip()
    {
        var report = CreateReport(CreateTripDto(), CreateTripDto(TripStatus.Completado));
        var sut = CreateSut();

        var bytes = sut.GenerateCsv(report);
        var text = Encoding.UTF8.GetString(bytes);
        var lines = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length); // header + 2 trips
        Assert.StartsWith("Fecha salida,", lines[0]);
        Assert.Contains(report.Trips[0].VehiclePatent!, lines[1]);
    }

    [Fact]
    public void GenerateCsv_StartsWithUtf8Bom()
    {
        var report = CreateReport(CreateTripDto());
        var sut = CreateSut();

        var bytes = sut.GenerateCsv(report);
        var bom = Encoding.UTF8.GetPreamble();

        Assert.Equal(bom, bytes.Take(bom.Length));
    }
}
