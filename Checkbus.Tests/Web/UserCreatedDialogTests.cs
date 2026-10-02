using System.Reflection;
using Checkbus.Web.Components.Dialogs;
using Microsoft.AspNetCore.Components;

namespace Checkbus.Tests.Web;

/// <summary>
/// Drift-detection coverage for <see cref="UserCreatedDialog"/>'s parameter surface. This repo has
/// no bUnit, so dialog content/rendering cannot be exercised directly — only its public contract
/// can be pinned via reflection, following the style of
/// <see cref="Checkbus.Tests.Users.WebContractEnumParityTests"/>. The future caller
/// (<c>RegisterUser.razor</c>, not built yet) supplies these four parameters independently from a
/// <c>RegisterUserResult</c> plus the admin-supplied document number, so this test guards against
/// the contract silently growing or shrinking.
/// </summary>
public class UserCreatedDialogTests
{
    private static readonly string[] ExpectedParameterNames = ["Email", "Name", "Surname", "InitialPassword"];

    [Fact]
    public void HasExactlyTheExpectedParameterProperties()
    {
        var parameterProperties = GetParameterProperties();

        var actualNames = parameterProperties.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal);
        var expectedNames = ExpectedParameterNames.OrderBy(n => n, StringComparer.Ordinal);

        Assert.Equal(expectedNames, actualNames);
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("Name")]
    [InlineData("Surname")]
    [InlineData("InitialPassword")]
    public void Parameter_IsRequiredStringDecoratedWithParameterAttribute(string parameterName)
    {
        var property = typeof(UserCreatedDialog).GetProperty(parameterName, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(property);
        Assert.Equal(typeof(string), property!.PropertyType);
        Assert.NotNull(property.GetCustomAttribute<ParameterAttribute>());

        var isRequired = property.GetCustomAttributes()
            .Any(attribute => attribute.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute");
        Assert.True(isRequired, $"Expected '{parameterName}' to be a 'required' member.");
    }

    private static List<PropertyInfo> GetParameterProperties()
    {
        return typeof(UserCreatedDialog)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
            .ToList();
    }
}
