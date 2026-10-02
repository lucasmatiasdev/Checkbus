namespace Checkbus.ApiService.Application.DriverRequirements.Queries
{
    // Server-side-only result: the controller uses FileKey to open the stream via
    // IFileStorageService and FileContentType for the response header — neither value is
    // ever serialized back to the client as JSON.
    public class DriverRequirementFileDto
    {
        public string? FileKey { get; set; }
        public string? FileContentType { get; set; }
    }
}
