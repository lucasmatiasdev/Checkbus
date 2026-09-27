using Checkbus.ApiService.Domain.Interfaces;

namespace Checkbus.Tests.Domain;

public class TenantEntityVisibilityTests
{
    [Fact]
    public void ITenantEntity_IsPublic()
    {
        Assert.True(typeof(ITenantEntity).IsPublic);
    }
}
