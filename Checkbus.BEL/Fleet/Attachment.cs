namespace Checkbus.BEL.Fleet;

/// <summary>
/// A file attached to exactly one owner. Designed for local-disk storage
/// (decision: local disk on the VPS, not S3-compatible object storage) — this
/// is a path/key reference, not a binary blob column; <c>IFileStorageService</c>
/// (Semana 3, follow-up PR) resolves <see cref="StoragePath"/> against the
/// disk, keyed by organization folder.
/// </summary>
/// <remarks>
/// Polymorphic ownership: exactly one of <see cref="VehicleDocumentationId"/>
/// or <see cref="DriverId"/> must be set (enforced by a database CHECK
/// constraint — see <c>AttachmentConfiguration</c>). The domain spec also
/// allows a <c>SupportTicket</c> owner, but that entity does not exist until
/// Semana 7; its owner column and the widened CHECK constraint are added
/// together with the <c>SupportTicket</c> entity in that later change.
/// </remarks>
public class Attachment
{
    public int Id { get; set; }

    public int? VehicleDocumentationId { get; set; }

    public VehicleDocumentation? VehicleDocumentation { get; set; }

    public int? DriverId { get; set; }

    public Driver? Driver { get; set; }

    /// <summary>Relative path/key on local disk, e.g. "{OrganizationId}/drivers/{DriverId}/license.pdf".</summary>
    public required string StoragePath { get; set; }

    public required string FileName { get; set; }

    /// <summary>File extension/format (e.g. "pdf", "jpg", "png").</summary>
    public required string Format { get; set; }

    public long SizeBytes { get; set; }

    public DateTime UploadedAt { get; set; }
}
