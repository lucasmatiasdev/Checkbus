namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Request-shaping concept only, not a persisted domain value — lives alongside the
    // query it belongs to rather than in Domain/Enums (same precedent as MercadoPagoOrderStatus
    // under Interfaces/Payments).
    public enum ReportExportFormat
    {
        Excel,
        Csv
    }
}
