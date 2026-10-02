namespace Checkbus.ApiService.Domain.Enums
{
    // Exactly 2 values by deliberate scope decision (see odd/tasks/driver-documents.md
    // Constraints #1): AptitudPsicofisica and AntecedentesPenales are verification-result
    // types handled outside this system during hiring and are intentionally never modeled
    // here. Do not add them.
    public enum DriverRequirementType
    {
        LicenciaConducir,
        CapacitacionProfesional
    }
}
