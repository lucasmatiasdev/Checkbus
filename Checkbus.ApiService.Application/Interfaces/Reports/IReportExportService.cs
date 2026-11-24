using Checkbus.ApiService.Application.Trips.Queries;

namespace Checkbus.ApiService.Application.Interfaces.Reports
{

    public interface IReportExportService
    {
        byte[] GenerateExcel(TripsReportDto report);
        byte[] GenerateCsv(TripsReportDto report);
    }
}
