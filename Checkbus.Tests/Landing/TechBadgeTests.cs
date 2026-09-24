using System.Reflection;
using Checkbus.Web.Components.Landing;
using Microsoft.AspNetCore.Components;

namespace Checkbus.Tests.Landing;

public class TechBadgeTests
{
    [Fact]
    public void TechBadge_HasLabelParameter()
    {
        var parameters = GetParameterProperties();

        Assert.Contains(parameters, p => p.Name == "Label" && p.PropertyType == typeof(string));
    }

    [Fact]
    public void TechBadge_ExposesNoImageOrIconParameter()
    {
        var parameters = GetParameterProperties();

        Assert.DoesNotContain(parameters, p =>
            p.Name.Contains("Icon", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("Image", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("Src", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("Logo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TechBadge_HasExactlyOneParameter()
    {
        var parameters = GetParameterProperties();

        Assert.Single(parameters);
    }

    private static PropertyInfo[] GetParameterProperties()
    {
        return typeof(TechBadge)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
            .ToArray();
    }
}
