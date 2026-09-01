namespace Checkbus.BEL.Auth;

/// <summary>
/// Fixed catalog of permission keys, in <c>module.action</c> form. This is the
/// single source of truth used both by <c>PermissionConfiguration</c> to seed
/// the <see cref="Permission"/> table and by later BLL authorization checks.
/// </summary>
public static class PermissionKeys
{
    public const string ReportsView = "reports.view";
    public const string ReportsManage = "reports.manage";
    public const string RolesManage = "roles.manage";
    public const string UsersManage = "users.manage";
    public const string RoutesManage = "routes.manage";
    public const string TripsManage = "trips.manage";
    public const string TripsDrive = "trips.drive";
    public const string VehiclesManage = "vehicles.manage";
    public const string IncidentsManage = "incidents.manage";
    public const string IncidentsValidate = "incidents.validate";

    /// <summary>All known permission keys, in seed order (fixed Ids 1..N).</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        ReportsView,
        ReportsManage,
        RolesManage,
        UsersManage,
        RoutesManage,
        TripsManage,
        TripsDrive,
        VehiclesManage,
        IncidentsManage,
        IncidentsValidate,
    };
}
