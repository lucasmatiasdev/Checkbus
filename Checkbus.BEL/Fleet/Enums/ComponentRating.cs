namespace Checkbus.BEL.Fleet.Enums;

/// <summary>
/// Health rating of a vehicle component, ordered from best to worst. The
/// numeric order matters: a <see cref="VehicleDiagnostic.OverallState"/> is
/// computed as the worst (highest-value) rating among its components.
/// </summary>
public enum ComponentRating
{
    Optimo = 0,
    Aceptable = 1,
    RequiereAtencion = 2,
    Critico = 3,
}
