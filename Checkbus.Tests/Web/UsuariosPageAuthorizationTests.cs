using Checkbus.Web.Components.Pages.app;
using Microsoft.AspNetCore.Authorization;

namespace Checkbus.Tests.Web;

/// <summary>
/// Drift-detection coverage for the admin-only Usuarios list page. This repo has no bUnit, so the
/// page cannot be rendered directly — only its compiled class's <see cref="AuthorizeAttribute"/>
/// can be pinned via reflection, following the style of
/// <see cref="RegisterUserPageAuthorizationTests"/>. This guards against the
/// <c>@attribute [Authorize(Roles = "Administrador")]</c> directive silently being removed or
/// loosened, which would let non-admin roles reach <c>/users</c>.
/// </summary>
public class UsuariosPageAuthorizationTests
{
    [Fact]
    public void HasAdministradorOnlyAuthorizeAttribute()
    {
        var attribute = typeof(Usuarios).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("Administrador", attribute!.Roles);
    }
}
