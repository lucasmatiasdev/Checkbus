using System.Collections.Generic;

namespace Checkbus.ApiService.Domain.Authorization
{
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Chofer = "Chofer";
        public const string Planificador = "Planificador";

        /// <summary>Identifier is ASCII-only by convention (safe in code search, routes,
        /// and log filters); the role's actual display/stored value keeps the accent.</summary>
        public const string Mecanico = "Mecánico";

        public static readonly IReadOnlyList<string> All =
        [
            Administrador,
            Chofer,
            Planificador,
            Mecanico
        ];
    }
}
