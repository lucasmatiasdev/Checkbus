using Checkbus.Web.Components.Pages.app;
using Microsoft.AspNetCore.Authorization;

namespace Checkbus.Tests.Web;

/// <summary>
/// Drift-detection coverage for the Planificador/Administrador-gated Payments settings page
/// (odd/tasks/pagos-mercadopago.md Fase I, I8) — same style as
/// <see cref="UsuariosPageAuthorizationTests"/>: this repo has no bUnit, so only the compiled
/// class's <see cref="AuthorizeAttribute"/> can be pinned via reflection. Guards against the
/// <c>@attribute [Authorize(Roles = "Planificador,Administrador")]</c> directive silently being
/// removed or loosened/narrowed — the role gate must match I7's start-connection endpoint's own
/// role gate, since there is no reason this page should show roles that can't use the button.
/// </summary>
public class PaymentsSettingsPageAuthorizationTests
{
    [Fact]
    public void HasPlanificadorOrAdministradorAuthorizeAttribute()
    {
        var attribute = typeof(PaymentsSettings).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("Planificador,Administrador", attribute!.Roles);
    }
}
