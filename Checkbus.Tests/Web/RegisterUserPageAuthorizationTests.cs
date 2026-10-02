using Checkbus.Web.Components.Pages.app;
using Microsoft.AspNetCore.Authorization;

namespace Checkbus.Tests.Web;

/// <summary>
/// Drift-detection coverage for the admin-only registration page. This repo has no bUnit, so the
/// page cannot be rendered directly — only its compiled class's <see cref="AuthorizeAttribute"/>
/// can be pinned via reflection, following the style of
/// <see cref="Checkbus.Tests.Users.UsersControllerAuthorizationTests"/> (applied here to a
/// Razor-compiled component instead of a controller). This guards against the
/// <c>@attribute [Authorize(Roles = "Administrador")]</c> directive silently being removed or
/// loosened, which would let non-admin roles reach <c>/users/register</c>.
/// </summary>
public class RegisterUserPageAuthorizationTests
{
    [Fact]
    public void HasAdministradorOnlyAuthorizeAttribute()
    {
        var attribute = typeof(RegisterUser).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("Administrador", attribute!.Roles);
    }
}
