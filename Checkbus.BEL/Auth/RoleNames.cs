namespace Checkbus.BEL.Auth;

/// <summary>
/// Names and fixed identifiers of the five predefined (global, shared)
/// roles seeded by <c>RoleConfiguration</c>. These rows always have
/// <see cref="Role.OrganizationId"/> equal to <c>null</c> and resolve to the
/// same <see cref="Role.Id"/> for every organization.
/// </summary>
public static class RoleNames
{
    public const string Planificador = "Planificador";
    public const string Mantenimiento = "Mantenimiento";
    public const string Validador = "Validador";
    public const string Chofer = "Chofer";
    public const string Administrador = "Administrador";

    public const int PlanificadorId = 1;
    public const int MantenimientoId = 2;
    public const int ValidadorId = 3;
    public const int ChoferId = 4;
    public const int AdministradorId = 5;
}
