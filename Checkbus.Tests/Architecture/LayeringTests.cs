using System.IO;
using System.Linq;
using System.Reflection;

namespace Checkbus.Tests.Architecture;

/// <summary>
/// Guards the layering rule from the design: UI depends only on BEL + BLL,
/// and BEL (the pure business-entity layer) never depends on EF Core.
/// </summary>
public class LayeringTests
{
    [Fact]
    public void Bel_Assembly_Does_Not_Reference_EntityFrameworkCore()
    {
        var belAssembly = Assembly.Load("Checkbus.BEL");

        var referencesEfCore = belAssembly.GetReferencedAssemblies()
            .Any(a => a.Name != null && a.Name.StartsWith("Microsoft.EntityFrameworkCore"));

        Assert.False(referencesEfCore);
    }

    [Fact]
    public void Ui_Assembly_Does_Not_Reference_Dal()
    {
        var testAssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var uiAssemblyPath = Path.GetFullPath(Path.Combine(
            testAssemblyDir, "..", "..", "..", "..", "Checkbus.UI", "bin", "Debug", "net8.0", "Checkbus.UI.dll"));
        var uiAssembly = Assembly.LoadFrom(uiAssemblyPath);

        var referencesDal = uiAssembly.GetReferencedAssemblies()
            .Any(a => a.Name == "Checkbus.DAL");

        Assert.False(referencesDal);
    }
}
