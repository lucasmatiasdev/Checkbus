using Checkbus.Web.Components.Pages.app;
using Microsoft.AspNetCore.Authorization;

namespace Checkbus.Tests.Web;

/// <summary>
/// Drift-detection coverage for the Chofer self-service "Mis Documentos" page. This repo has no
/// bUnit, so the page cannot be rendered directly — only its compiled class's
/// <see cref="AuthorizeAttribute"/> can be pinned via reflection, following the style of
/// <see cref="RegisterUserPageAuthorizationTests"/>. This guards against the
/// <c>@attribute [Authorize(Roles = "Chofer")]</c> directive silently being removed or loosened,
/// which would let other roles reach <c>/my-documents</c>.
/// </summary>
public class MyDocumentsPageAuthorizationTests
{
    [Fact]
    public void HasChoferOnlyAuthorizeAttribute()
    {
        var attribute = typeof(MyDocuments).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("Chofer", attribute!.Roles);
    }
}
