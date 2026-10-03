namespace Checkbus.ApiService.Application.VehicleDocuments.Queries
{
    // Server-side-only result: the controller uses FileKey to open the stream via
    // IFileStorageService and FileContentType for the response header — neither value is
    // ever serialized back to the client as JSON. Mirrors DriverRequirementFileDto.
    public class VehicleDocumentFileDto
    {
        public string? FileKey { get; set; }
        public string? FileContentType { get; set; }
    }
}
