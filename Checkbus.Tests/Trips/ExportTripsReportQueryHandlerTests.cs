using Checkbus.ApiService.Application.Interfaces.Reports;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.Tests.Trips;

public class ExportTripsReportQueryHandlerTests
{
    private sealed class FakeSender(TripsReportDto reportToReturn) : ISender
    {
        public GetTripsReportQuery? ReceivedQuery { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            ReceivedQuery = Assert.IsType<GetTripsReportQuery>(request);
            return Task.FromResult((TResponse)(object)reportToReturn);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Not needed by ExportTripsReportQueryHandler.");

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => throw new NotSupportedException("Not needed by ExportTripsReportQueryHandler.");

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Not needed by ExportTripsReportQueryHandler.");

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Not needed by ExportTripsReportQueryHandler.");
    }

    private sealed class FakeReportExportService(byte[] excelBytes, byte[] csvBytes) : IReportExportService
    {
        public TripsReportDto? ReceivedReport { get; private set; }
        public int ExcelCalls { get; private set; }
        public int CsvCalls { get; private set; }

        public byte[] GenerateExcel(TripsReportDto report)
        {
            ReceivedReport = report;
            ExcelCalls++;
            return excelBytes;
        }

        public byte[] GenerateCsv(TripsReportDto report)
        {
            ReceivedReport = report;
            CsvCalls++;
            return csvBytes;
        }
    }

    private static TripsReportDto EmptyReport() => new()
    {
        Trips = [],
        Statistics = new TripsReportStatisticsDto()
    };

    [Fact]
    public async Task Handle_ExcelFormat_CallsGenerateExcelAndReturnsXlsxContentType()
    {
        var report = EmptyReport();
        var excelBytes = new byte[] { 1, 2, 3 };
        var sender = new FakeSender(report);
        var exportService = new FakeReportExportService(excelBytes, []);
        var handler = new ExportTripsReportQueryHandler(sender, exportService);

        var result = await handler.Handle(
            new ExportTripsReportQuery { Format = ReportExportFormat.Excel },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, exportService.ExcelCalls);
        Assert.Equal(0, exportService.CsvCalls);
        Assert.Same(excelBytes, result.Content);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
        Assert.EndsWith(".xlsx", result.FileName);
    }

    [Fact]
    public async Task Handle_CsvFormat_CallsGenerateCsvAndReturnsCsvContentType()
    {
        var report = EmptyReport();
        var csvBytes = new byte[] { 4, 5, 6 };
        var sender = new FakeSender(report);
        var exportService = new FakeReportExportService([], csvBytes);
        var handler = new ExportTripsReportQueryHandler(sender, exportService);

        var result = await handler.Handle(
            new ExportTripsReportQuery { Format = ReportExportFormat.Csv },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, exportService.CsvCalls);
        Assert.Equal(0, exportService.ExcelCalls);
        Assert.Same(csvBytes, result.Content);
        Assert.Equal("text/csv", result.ContentType);
        Assert.EndsWith(".csv", result.FileName);
    }

    [Fact]
    public async Task Handle_ForwardsFiltersToGetTripsReportQuery()
    {
        var report = EmptyReport();
        var sender = new FakeSender(report);
        var exportService = new FakeReportExportService([], []);
        var handler = new ExportTripsReportQueryHandler(sender, exportService);

        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        await handler.Handle(
            new ExportTripsReportQuery
            {
                Format = ReportExportFormat.Excel,
                DateFrom = new DateOnly(2026, 1, 1),
                DateTo = new DateOnly(2026, 12, 31),
                Status = TripStatus.Completado,
                DriverId = driverId,
                VehicleId = vehicleId
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(sender.ReceivedQuery);
        Assert.Equal(new DateOnly(2026, 1, 1), sender.ReceivedQuery!.DateFrom);
        Assert.Equal(new DateOnly(2026, 12, 31), sender.ReceivedQuery.DateTo);
        Assert.Equal(TripStatus.Completado, sender.ReceivedQuery.Status);
        Assert.Equal(driverId, sender.ReceivedQuery.DriverId);
        Assert.Equal(vehicleId, sender.ReceivedQuery.VehicleId);
    }
}
