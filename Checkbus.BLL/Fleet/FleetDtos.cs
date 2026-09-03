using Checkbus.BEL.Fleet.Enums;

namespace Checkbus.BLL.Fleet;

/// <summary>A single component rating entered as part of a Vehicle's diagnostic (S3.7 onboarding).</summary>
public record ComponentRatingInput(VehicleComponent Component, ComponentRating Rating);

/// <summary>Minimal Vehicle projection for list screens (e.g. the maintenance-record entry picker).</summary>
public record VehicleSummary(int Id, string Plate, int Capacity, ComponentRating? CurrentDiagnosticState);

/// <summary>
/// A file selected in the UI, ready to be persisted via <see cref="Checkbus.BLL.Storage.IFileStorageService"/>.
/// <paramref name="Content"/> must report a valid <see cref="Stream.Length"/> before being passed in.
/// </summary>
public record FileUpload(string FileName, string Format, Stream Content);
