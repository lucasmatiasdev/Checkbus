using Checkbus.ApiService.Application.Interfaces.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Checkbus.ApiService.Infrastructure.Implementations.Email
{
    /// <summary>
    /// <see cref="IEmailService"/> adapter sending mail via MailKit's <see cref="SmtpClient"/>
    /// against an SMTP relay (Brevo's free tier, 300 emails/day — odd/tasks/reservas-publicas.md
    /// Fase 2). MailKit decouples the transport from the provider: swapping SMTP hosts later is
    /// config-only, not a code change. Same error-wrapping convention as
    /// <c>GoogleDirectionsService</c>: wraps MailKit's own exceptions in
    /// <see cref="InvalidOperationException"/> rather than letting them leak into the handler.
    /// </summary>
    public class SmtpEmailService(EmailOptions options) : IEmailService
    {
        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            using var client = new SmtpClient();
            try
            {
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
                mimeMessage.To.Add(MailboxAddress.Parse(message.To));
                mimeMessage.Subject = message.Subject;
                mimeMessage.Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody();

                await client.ConnectAsync(
                    options.Host,
                    options.Port,
                    options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                    cancellationToken);

                if (!string.IsNullOrEmpty(options.Username))
                {
                    await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);
                }

                await client.SendAsync(mimeMessage, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Catches every MailKit/MimeKit/transport failure mode: bad host/port
                // (ArgumentException/SocketException), SMTP protocol/command errors
                // (SmtpCommandException/SmtpProtocolException), auth failures
                // (AuthenticationException), and malformed addresses (FormatException/
                // ParseException) — same posture as GoogleDirectionsService: never let a
                // provider-specific exception type leak out of this adapter.
                throw new InvalidOperationException("Failed to send the email via the SMTP relay.", ex);
            }
        }
    }
}
