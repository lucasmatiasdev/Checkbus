namespace Checkbus.BEL.Fleet.Enums;

/// <summary>
/// Lifecycle status of a <see cref="MaintenanceRecord"/>. Not explicitly
/// enumerated by the domain spec (which only names the attribute "status");
/// this is the minimal set needed to track a maintenance event end-to-end.
/// </summary>
public enum MaintenanceStatus
{
    Programado,
    EnProceso,
    Completado,
    Cancelado,
}
