namespace Checkbus.ApiService.Application.Events.Queries
{
    // Server-side-only result, mirrors VehicleDocumentFileDto: the controller uses ImageKey to
    // open the stream via IFileStorageService and ImageContentType for the response header —
    // neither value is ever serialized back to the client as JSON.
    public class EventImageFileDto
    {
        public string? ImageKey { get; set; }
        public string? ImageContentType { get; set; }
    }
}
