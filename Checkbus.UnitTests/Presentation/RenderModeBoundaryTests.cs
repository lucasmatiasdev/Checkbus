using System.Reflection;
using Checkbus.Presentation.Components.Layout;
using Checkbus.Presentation.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Checkbus.UnitTests.Presentation
{
    /// <summary>
    /// Reflection invariants over the compiled render-mode/layout boundary (design D-F1/D-F3, in
    /// the style of <c>TenantFilterArchitectureTests</c>). Login must stay static SSR under a
    /// static <see cref="AuthLayout"/> so <c>HttpContext.SignInAsync</c> can still write response
    /// headers before the response starts; the still-static <see cref="MainLayout"/> hosts the
    /// interactive island (<see cref="InteractiveShell"/>) as a sibling of <c>@Body</c>, never as
    /// an ancestor that would force a circuit onto every routed page.
    /// </summary>
    public class RenderModeBoundaryTests
    {
        [Fact]
        public void Login_HasNoRenderModeAttribute()
        {
            var renderModeAttributes = typeof(Login).GetCustomAttributes(inherit: false)
                .OfType<RenderModeAttribute>()
                .ToList();

            Assert.Empty(renderModeAttributes);
        }

        [Fact]
        public void Login_UsesAuthLayout()
        {
            var layoutAttribute = typeof(Login).GetCustomAttribute<LayoutAttribute>();

            Assert.NotNull(layoutAttribute);
            Assert.Equal(typeof(AuthLayout), layoutAttribute!.LayoutType);
        }

        [Fact]
        public void AuthLayout_HasNoRenderModeAttribute()
        {
            var renderModeAttributes = typeof(AuthLayout).GetCustomAttributes(inherit: false)
                .OfType<RenderModeAttribute>()
                .ToList();

            Assert.Empty(renderModeAttributes);
        }

        [Fact]
        public void MainLayout_HasNoRenderModeAttribute()
        {
            var renderModeAttributes = typeof(MainLayout).GetCustomAttributes(inherit: false)
                .OfType<RenderModeAttribute>()
                .ToList();

            Assert.Empty(renderModeAttributes);
        }

        [Fact]
        public void InteractiveShell_DeclaresInteractiveServerRenderMode()
        {
            var renderModeAttribute = typeof(InteractiveShell).GetCustomAttributes(inherit: false)
                .OfType<RenderModeAttribute>()
                .Single();

            Assert.IsType<InteractiveServerRenderMode>(renderModeAttribute.Mode);
        }
    }
}
