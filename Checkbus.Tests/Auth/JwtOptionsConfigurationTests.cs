using Microsoft.Extensions.Configuration;

namespace Checkbus.Tests.Auth;

public class JwtOptionsConfigurationTests
{
    private static IConfigurationRoot LoadApiServiceConfiguration()
    {
        var appsettingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Checkbus.ApiService",
            "appsettings.json");

        return new ConfigurationBuilder()
            .AddJsonFile(Path.GetFullPath(appsettingsPath), optional: false)
            .Build();
    }

    [Fact]
    public void JwtExpirationMinutes_IsConfiguredAsFourHours()
    {
        var configuration = LoadApiServiceConfiguration();

        var expirationMinutes = configuration["Jwt:ExpirationMinutes"];

        Assert.Equal("240", expirationMinutes);
    }
}
