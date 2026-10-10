namespace Checkbus.ApiService.Application.Interfaces.Email
{
    /// <summary>Transport-agnostic email payload. The Application layer decides what the HTML
    /// body means (ticket content, copy, links); Infrastructure only knows how to send it —
    /// same separation <c>IFileStorageService</c> keeps between opaque bytes and their meaning.</summary>
    public record EmailMessage(string To, string Subject, string HtmlBody);
}
