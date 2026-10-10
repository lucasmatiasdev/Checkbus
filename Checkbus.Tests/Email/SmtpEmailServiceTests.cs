using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Infrastructure.Implementations.Email;

namespace Checkbus.Tests.Email;

/// <summary>
/// Proves <see cref="SmtpEmailService"/> wraps every MailKit/transport failure in
/// <see cref="InvalidOperationException"/> (odd/tasks/reservas-publicas.md Fase 2, M1), same
/// error-wrapping convention as <c>GoogleDirectionsService</c>. Does NOT hit a real SMTP
/// server — same spirit as <c>GoogleDirectionsServiceTests</c> not calling the real Google API:
/// a deliberately blank/misconfigured <see cref="EmailOptions.Host"/> is enough to force the
/// connect attempt to fail fast without any network dependency in CI.
/// </summary>
public class SmtpEmailServiceTests
{
    [Fact]
    public async Task SendAsync_BlankHost_ThrowsInvalidOperationException()
    {
        var options = new EmailOptions
        {
            Host = string.Empty,
            Port = 587,
            FromAddress = "no-reply@checkbus.test",
            FromName = "Checkbus"
        };
        var service = new SmtpEmailService(options);
        var message = new EmailMessage("passenger@checkbus.test", "Subject", "<p>Body</p>");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendAsync(message, TestContext.Current.CancellationToken));
    }
}
