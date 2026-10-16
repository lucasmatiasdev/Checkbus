using System.Globalization;
using System.Text;
using Checkbus.ApiService.Application.Interfaces.Reports;
using Checkbus.ApiService.Application.Trips.Queries;
using ClosedXML.Excel;

namespace Checkbus.ApiService.Infrastructure.Implementations.Reports
{
    public class ReportExportService : IReportExportService
    {
        private static readonly string[] ColumnHeaders =
        [
            "Fecha salida", "Fecha llegada programada", "Estado", "Vehiculo (patente)",
            "Chofer", "Evento", "Capacidad", "Asientos disponibles", "Precio"
        ];

        public byte[] GenerateExcel(TripsReportDto report)
        {
            using var workbook = new XLWorkbook();

            var tripsSheet = workbook.Worksheets.Add("Viajes");
            WriteHeader(tripsSheet, ColumnHeaders);
            for (var i = 0; i < report.Trips.Count; i++)
            {
                WriteTripRow(tripsSheet, i + 2, report.Trips[i]);
            }
            tripsSheet.Columns().AdjustToContents();

            var statsSheet = workbook.Worksheets.Add("Estadisticas");
            statsSheet.Cell(1, 1).Value = "Total de viajes";
            statsSheet.Cell(1, 2).Value = report.Statistics.TotalTrips;
            statsSheet.Cell(2, 1).Value = "Total de pasajeros";
            statsSheet.Cell(2, 2).Value = report.Statistics.TotalPassengers;
            statsSheet.Cell(3, 1).Value = "Ingreso estimado";
            statsSheet.Cell(3, 2).Value = report.Statistics.EstimatedRevenue;

            var row = 5;
            statsSheet.Cell(row, 1).Value = "Viajes por estado";
            row++;
            foreach (var (status, count) in report.Statistics.TripsByStatus)
            {
                statsSheet.Cell(row, 1).Value = status.ToString();
                statsSheet.Cell(row, 2).Value = count;
                row++;
            }
            statsSheet.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] GenerateCsv(TripsReportDto report)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(',', ColumnHeaders.Select(EscapeCsvField)));

            foreach (var trip in report.Trips)
            {
                var fields = new[]
                {
                    trip.DepartureDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    trip.ArrivalDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    trip.Status.ToString(),
                    trip.VehiclePatent ?? string.Empty,
                    $"{trip.DriverName} {trip.DriverSurname}".Trim(),
                    trip.EventName ?? string.Empty,
                    trip.Capacity.ToString(CultureInfo.InvariantCulture),
                    trip.AvailableSeats.ToString(CultureInfo.InvariantCulture),
                    trip.Price.ToString(CultureInfo.InvariantCulture)
                };
                sb.AppendLine(string.Join(',', fields.Select(EscapeCsvField)));
            }

            // Leading BOM so Excel on Windows/es-AR opens the file as UTF-8 instead of
            // mis-detecting it as the system codepage (breaks accented characters otherwise).
            var bom = Encoding.UTF8.GetPreamble();
            var textBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var result = new byte[bom.Length + textBytes.Length];
            bom.CopyTo(result, 0);
            textBytes.CopyTo(result, bom.Length);
            return result;
        }

        private static void WriteHeader(IXLWorksheet sheet, string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
            }
        }

        private static void WriteTripRow(IXLWorksheet sheet, int row, TripDto trip)
        {
            sheet.Cell(row, 1).Value = trip.DepartureDate;
            sheet.Cell(row, 2).Value = trip.ArrivalDate;
            sheet.Cell(row, 3).Value = trip.Status.ToString();
            sheet.Cell(row, 4).Value = trip.VehiclePatent ?? string.Empty;
            sheet.Cell(row, 5).Value = $"{trip.DriverName} {trip.DriverSurname}".Trim();
            sheet.Cell(row, 6).Value = trip.EventName ?? string.Empty;
            sheet.Cell(row, 7).Value = trip.Capacity;
            sheet.Cell(row, 8).Value = trip.AvailableSeats;
            sheet.Cell(row, 9).Value = trip.Price;
        }

        // Minimal RFC 4180 quoting — only the delimiter, quote char and newlines force quoting,
        // same as every other hand-written CSV writer; no CsvHelper dependency for this.
        private static string EscapeCsvField(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}
