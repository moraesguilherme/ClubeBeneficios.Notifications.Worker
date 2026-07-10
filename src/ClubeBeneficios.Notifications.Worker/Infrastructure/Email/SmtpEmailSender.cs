using System.Net;
using System.Net.Mail;
using System.Text;
using ClubeBeneficios.Notifications.Worker.Configuration;
using ClubeBeneficios.Notifications.Worker.Models;
using Microsoft.Extensions.Options;

namespace ClubeBeneficios.Notifications.Worker.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<NotificationSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateOptions();
            ValidateMessage(message);

            using var mailMessage = BuildMailMessage(message);
            using var smtpClient = BuildSmtpClient();

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            var providerMessageId = mailMessage.Headers["Message-ID"];

            _logger.LogInformation(
                "E-mail enviado para {RecipientEmail}. Subject={Subject}",
                message.ToEmail,
                message.Subject);

            return NotificationSendResult.Sent(providerMessageId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao enviar e-mail para {RecipientEmail}. Subject={Subject}",
                message.ToEmail,
                message.Subject);

            return NotificationSendResult.Failed(ex.Message);
        }
    }

    private MailMessage BuildMailMessage(EmailMessage message)
    {
        var from = string.IsNullOrWhiteSpace(_options.FromName)
            ? new MailAddress(_options.FromEmail)
            : new MailAddress(_options.FromEmail, _options.FromName, Encoding.UTF8);

        var to = string.IsNullOrWhiteSpace(message.ToName)
            ? new MailAddress(message.ToEmail)
            : new MailAddress(message.ToEmail, message.ToName, Encoding.UTF8);

        var mailMessage = new MailMessage
        {
            From = from,
            Subject = message.Subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            HeadersEncoding = Encoding.UTF8,
            IsBodyHtml = !string.IsNullOrWhiteSpace(message.BodyHtml),
            Body = !string.IsNullOrWhiteSpace(message.BodyHtml)
                ? message.BodyHtml
                : message.BodyText ?? string.Empty
        };

        mailMessage.To.Add(to);

        if (!string.IsNullOrWhiteSpace(message.BodyText) && !string.IsNullOrWhiteSpace(message.BodyHtml))
        {
            var textView = AlternateView.CreateAlternateViewFromString(
                message.BodyText,
                Encoding.UTF8,
                "text/plain");

            var htmlView = AlternateView.CreateAlternateViewFromString(
                message.BodyHtml,
                Encoding.UTF8,
                "text/html");

            mailMessage.AlternateViews.Add(textView);
            mailMessage.AlternateViews.Add(htmlView);
        }

        return mailMessage;
    }

    private SmtpClient BuildSmtpClient()
    {
        var smtpClient = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            smtpClient.Credentials = new NetworkCredential(
                _options.UserName,
                _options.Password);
        }

        return smtpClient;
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException("Smtp:Host não configurado.");
        }

        if (_options.Port <= 0)
        {
            throw new InvalidOperationException("Smtp:Port inválido.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("Smtp:FromEmail não configurado.");
        }
    }

    private static void ValidateMessage(EmailMessage message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.ToEmail))
        {
            throw new InvalidOperationException("Destinatário do e-mail não informado.");
        }

        if (string.IsNullOrWhiteSpace(message.Subject))
        {
            throw new InvalidOperationException("Assunto do e-mail não informado.");
        }

        if (string.IsNullOrWhiteSpace(message.BodyHtml) && string.IsNullOrWhiteSpace(message.BodyText))
        {
            throw new InvalidOperationException("Corpo do e-mail não informado.");
        }
    }
}
