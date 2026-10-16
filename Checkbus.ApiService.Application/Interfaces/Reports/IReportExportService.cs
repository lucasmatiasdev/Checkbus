using Checkbus.ApiService.Application.Trips.Queries;

namespace Checkbus.ApiService.Application.Interfaces.Reports
{
    // Synchronous on purpose — both implementations build the output entirely in memory
    // from an already-materialized DTO, no I/O to await.
    public interface IReportExportService
    {
        byte[] GenerateExcel(TripsReportDto report);
        byte[] GenerateCsv(TripsReportDto report);
    }
}
