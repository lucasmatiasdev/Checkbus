using Checkbus.ApiService.Application.Interfaces.Reports;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class ExportTripsReportQueryHandler : IRequestHandler<ExportTripsReportQuery, ExportedReportDto>
    {
        private readonly ISender _sender;
        private readonly IReportExportService _exportService;

        public ExportTripsReportQueryHandler(ISender sender, IReportExportService exportService)
        {
            _sender = sender;
            _exportService = exportService;
        }

        public async Task<ExportedReportDto> Handle(ExportTripsReportQuery request, CancellationToken cancellationToken)
        {
            // Reuses GetTripsReportQueryHandler's filtering/joins/statistics via mediator
            // instead of duplicating that logic here.
            var report = await _sender.Send(
                new GetTripsReportQuery
                {
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    Status = request.Status,
                    DriverId = request.DriverId,
                    VehicleId = request.VehicleId
                },
                cancellationToken);

            var today = DateTime.UtcNow.ToString("yyyyMMdd");

            return request.Format switch
            {
                ReportExportFormat.Excel => new ExportedReportDto
                {
                    Content = _exportService.GenerateExcel(report),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileName = $"reporte-viajes-{today}.xlsx"
                },
                ReportExportFormat.Csv => new ExportedReportDto
                {
                    Content = _exportService.GenerateCsv(report),
                    ContentType = "text/csv",
                    FileName = $"reporte-viajes-{today}.csv"
                },
                _ => throw new ArgumentOutOfRangeException(nameof(request), request.Format, "Unsupported export format.")
            };
        }
    }
}
