using System.Reflection;
using Checkbus.Presentation.Components;
using Checkbus.Presentation.Components.Layout;
using Checkbus.Presentation.Components.Pages;
using Checkbus.Presentation.Components.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Checkbus.UnitTests.Presentation
{
    /// <summary>
    /// Reflection invariants over the compiled render-mode/layout boundary (design D-F1/D-F3/D-PF10,
    /// in the style of <c>TenantFilterArchitectureTests</c>). Login and Home must stay static SSR
    /// under a content-free <see cref="EmptyLayout"/> so neither opens a circuit; the still-static
    /// <see cref="MainLayout"/> hosts two interactive islands (<see cref="InteractiveShell"/> and
    /// <see cref="Sidebar"/>) as siblings of <c>@Body</c>, never as an ancestor that would force a
    /// circuit onto every routed page. Dashboard is the first page routed through
    /// <see cref="MainLayout"/> (no page-level <c>@layout</c> override), so it is the first to
    /// inherit interactivity via those islands rather than staying static under
    /// <see cref="EmptyLayout"/>.
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
        public void Login_UsesEmptyLayout()
        {
            var layoutAttribute = typeof(Login).GetCustomAttribute<LayoutAttribute>();

            Assert.NotNull(layoutAttribute);
            Assert.Equal(typeof(EmptyLayout), layoutAttribute!.LayoutType);
        }

        [Fact]
        public void EmptyLayout_HasNoRenderModeAttribute()
        {
            var renderModeAttributes = typeof(EmptyLayout).GetCustomAttributes(inherit: false)
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

        [Fact]
        public void Home_UsesEmptyLayout()
        {
            var layoutAttribute = typeof(Home).GetCustomAttribute<LayoutAttribute>();

            Assert.NotNull(layoutAttribute);
            Assert.Equal(typeof(EmptyLayout), layoutAttribute!.LayoutType);
        }

        [Fact]
        public void Home_HasNoRenderModeAttribute()
        {
            var renderModeAttributes = typeof(Home).GetCustomAttributes(inherit: false)
                .OfType<RenderModeAttribute>()
                .ToList();

            Assert.Empty(renderModeAttributes);
        }

        [Fact]
        public void SiteNav_HasNoRenderModeAttribute()
        {
            var renderModeAttributes = typeof(SiteNav).GetCustomAttributes(inherit: false)
                .OfType<RenderModeAttribute>()
                .ToList();

            Assert.Empty(renderModeAttributes);
        }

        [Fact]
        public void SiteNav_HasNoLayoutAttribute()
        {
            var layoutAttribute = typeof(SiteNav).GetCustomAttribute<LayoutAttribute>();

            Assert.Null(layoutAttribute);
        }

        [Fact]
        public void Dashboard_HasNoRenderModeAttribute()
        {
            var renderModeAttributes = typeof(Dashboard).GetCustomAttributes(inherit: false)
                .OfType<RenderModeAttribute>()
                .ToList();

            Assert.Empty(renderModeAttributes);
        }

        [Fact]
        public void Dashboard_HasNoLayoutAttribute()
        {
            var layoutAttribute = typeof(Dashboard).GetCustomAttribute<LayoutAttribute>();

            Assert.Null(layoutAttribute);
        }

        [Fact]
        public void Dashboard_HasAuthorizeAttribute()
        {
            Assert.NotNull(typeof(Dashboard).GetCustomAttribute<AuthorizeAttribute>());
        }

        [Fact]
        public void Sidebar_DeclaresInteractiveServerRenderMode()
        {
            var renderModeAttribute = typeof(Sidebar).GetCustomAttributes(inherit: false)
                .OfType<RenderModeAttribute>()
                .Single();

            Assert.IsType<InteractiveServerRenderMode>(renderModeAttribute.Mode);
        }

        [Fact]
        public void Sidebar_HasNoLayoutAttribute()
        {
            var layoutAttribute = typeof(Sidebar).GetCustomAttribute<LayoutAttribute>();

            Assert.Null(layoutAttribute);
        }

        [Fact]
        public void InteractiveIslands_AreExactlyInteractiveShellAndSidebar()
        {
            var interactiveIslands = typeof(App).Assembly.GetTypes()
                .Where(type => typeof(IComponent).IsAssignableFrom(type))
                .Where(type => type.GetCustomAttributes(inherit: false).OfType<RenderModeAttribute>().Any())
                .ToList();

            Assert.Equal(
                new[] { typeof(InteractiveShell), typeof(Sidebar) }.OrderBy(type => type.FullName),
                interactiveIslands.OrderBy(type => type.FullName));
        }
    }
}
