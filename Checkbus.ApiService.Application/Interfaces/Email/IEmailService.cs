namespace Checkbus.ApiService.Application.Interfaces.Email
{
    /// <summary>Generic email transport. Deliberately NOT a domain-specific
    /// <c>SendReservationConfirmation(...)</c> method — Infrastructure only knows how to send an
    /// already-built <see cref="EmailMessage"/>; building ticket/confirmation HTML content is the
    /// Application handler's job.</summary>
    public interface IEmailService
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
    }
}
