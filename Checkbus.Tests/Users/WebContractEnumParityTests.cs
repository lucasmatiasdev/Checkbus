using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.Web.Contracts;

namespace Checkbus.Tests.Users;

/// <summary>
/// Drift-detection coverage for the Checkbus.Web BFF mirror enums (<see cref="WebRole"/>,
/// <see cref="WebDocumentType"/>). Checkbus.Web never references the API projects, so these
/// enums are hand-duplicated; this test walks the real domain enums via reflection and asserts
/// every member has a same-named counterpart with an identical underlying numeric value on the
/// web side, catching future additions, removals, or renumbering on either side.
/// </summary>
public class WebContractEnumParityTests
{
    [Fact]
    public void WebRole_StaysNumericallyInSyncWith_RealRole()
    {
        AssertEnumParity<Role, WebRole>();
    }

    [Fact]
    public void WebDocumentType_StaysNumericallyInSyncWith_RealDocumentType()
    {
        AssertEnumParity<DocumentType, WebDocumentType>();
    }

    private static void AssertEnumParity<TReal, TWeb>()
        where TReal : struct, Enum
        where TWeb : struct, Enum
    {
        var realNames = Enum.GetNames<TReal>();
        var webNames = Enum.GetNames<TWeb>();

        Assert.Equal(realNames.OrderBy(n => n, StringComparer.Ordinal), webNames.OrderBy(n => n, StringComparer.Ordinal));

        foreach (var name in realNames)
        {
            var realValue = Convert.ToInt64(Enum.Parse<TReal>(name));
            var webValue = Convert.ToInt64(Enum.Parse<TWeb>(name));

            Assert.True(
                realValue == webValue,
                $"Member '{name}' has value {realValue} on {typeof(TReal).FullName} but {webValue} on {typeof(TWeb).FullName}.");
        }
    }
}
