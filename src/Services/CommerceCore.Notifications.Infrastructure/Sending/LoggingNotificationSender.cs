using CommerceCore.Notifications.Application;
using CommerceCore.Notifications.Domain;
using Microsoft.Extensions.Logging;

namespace CommerceCore.Notifications.Infrastructure.Sending;

// Stands in for an SMTP or email-API client. Replace this one class to send real email.
internal sealed class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(Notification notification, CancellationToken ct = default)
    {
        logger.LogInformation("Sending {Kind} email to {Recipient}: {Subject} | {Body}",
            notification.Kind, notification.Recipient, notification.Subject, notification.Body);

        return Task.CompletedTask;
    }
}
