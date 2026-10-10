using Checkbus.ApiService.Infrastructure.Implementations.Email;

namespace Checkbus.Tests.Email;

/// <summary>
/// Proves <see cref="EmailOptions"/>'s permissive-default shape (odd/tasks/reservas-publicas.md
/// Fase 2, M1) — mirrors <c>GoogleMapsOptions</c>: a brand-new instance (as built by
/// <c>Program.cs</c>'s <c>?? new EmailOptions()</c> fallback when the "Email" config section is
/// missing) must have blank string fields and sane SMTP defaults, never throw/require
/// construction arguments.
/// </summary>
public class EmailOptionsTests
{
    [Fact]
    public void DefaultConstruction_HasBlankStringsAndSaneSmtpDefaults()
    {
        var options = new EmailOptions();

        Assert.Equal(string.Empty, options.Host);
        Assert.Equal(string.Empty, options.Username);
        Assert.Equal(string.Empty, options.Password);
        Assert.Equal(string.Empty, options.FromAddress);
        Assert.Equal(string.Empty, options.FromName);
        Assert.Equal(587, options.Port);
        Assert.True(options.UseStartTls);
    }
}
