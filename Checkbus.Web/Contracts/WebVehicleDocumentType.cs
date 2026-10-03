namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Domain.Enums.VehicleDocumentType</c>. Checkbus.Web
/// does not reference the API projects (pure BFF), so this enum is duplicated here rather than
/// shared. Member names and underlying numeric values must stay identical to the real
/// <c>VehicleDocumentType</c> enum — no <c>JsonStringEnumConverter</c> is registered anywhere, so
/// the wire value is a plain integer that depends on member order matching exactly. The first two
/// members (<see cref="Seguro"/>, <see cref="RTO_VTV"/>) are the universal types auto-created on
/// vehicle registration; the rest are conditional types created on demand at first upload.
/// </summary>
public enum WebVehicleDocumentType
{
    Seguro,
    RTO_VTV,
    TituloPropiedad,
    LeasingInscripto,
    ContratoAlquiler,
    HabilitacionEspecifica
}
