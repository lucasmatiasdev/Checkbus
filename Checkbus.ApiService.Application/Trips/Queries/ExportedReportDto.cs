namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class ExportedReportDto
    {
        public required byte[] Content { get; set; }
        public required string ContentType { get; set; }
        public required string FileName { get; set; }
    }
}
