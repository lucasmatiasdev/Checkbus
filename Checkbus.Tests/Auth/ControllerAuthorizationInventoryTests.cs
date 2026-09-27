using System.Reflection;
using Checkbus.ApiService.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Auth;

/// <summary>
/// Proves the scope of the global <c>AuthorizeFilter</c> registered in
/// <c>Checkbus.ApiService/Program.cs</c>: every controller action in the API is
/// authenticated by default unless explicitly, deliberately exempted. These tests
/// enumerate the actual controller/action surface via reflection so that adding a
/// new controller or a new <see cref="AllowAnonymousAttribute"/> action forces this
/// test to be updated instead of silently changing the default-deny posture.
/// </summary>
public class ControllerAuthorizationInventoryTests
{
    private static readonly Assembly ApiAssembly = typeof(AuthController).Assembly;

    private static IEnumerable<Type> DiscoverControllerTypes() =>
        ApiAssembly.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

    private static IEnumerable<MethodInfo> DiscoverActionMethods(Type controllerType) =>
        controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && m.GetCustomAttribute<NonActionAttribute>() is null);

    [Fact]
    public void ProductionControllerInventory_MatchesKnownSet()
    {
        var controllerTypes = DiscoverControllerTypes()
            .Select(t => t.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["AuthController"], controllerTypes);
    }

    [Fact]
    public void AnonymousActionInventory_MatchesKnownAllowlist()
    {
        var anonymousActions = DiscoverControllerTypes()
            .SelectMany(t => DiscoverActionMethods(t)
                .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null
                    || t.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
                .Select(m => $"{t.Name}.{m.Name}"))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["AuthController.Login"], anonymousActions);
    }
}
