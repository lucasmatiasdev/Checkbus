namespace Checkbus.ApiService.Application.Interfaces.Email
{
    /// <summary>Transport-agnostic email payload. The Application layer decides what the HTML
    /// body means (ticket content, copy, links); Infrastructure only knows how to send it —
    /// same separation <c>IFileStorageService</c> keeps between opaque bytes and their meaning.
    /// <paramref name="InlineImages"/> is optional and defaults to none — every existing caller
    /// building a plain HTML email is unaffected.</summary>
    public record EmailMessage(
        string To,
        string Subject,
        string HtmlBody,
        IReadOnlyList<EmailInlineImage>? InlineImages = null);

    /// <summary>
    /// An image embedded directly in the HTML body via a MIME <c>cid:</c> reference (not a
    /// regular downloadable attachment) — the <see cref="HtmlBody"/> that carries one must
    /// reference it as <c>&lt;img src="cid:{ContentId}"&gt;</c>.
    /// </summary>
    public record EmailInlineImage(string ContentId, byte[] Content, string MimeType);
}
