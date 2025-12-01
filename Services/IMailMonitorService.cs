using MailUptime.Models;

namespace MailUptime.Services;

public interface IMailUptimeService
{
    MailCheckResult GetMailStatus(string mailboxName);
    Task StartMonitoringAsync(CancellationToken cancellationToken);
    IEnumerable<string> GetAllMailboxNames();
    IEnumerable<MailboxStatusDto> GetAllMailboxStatuses();
}
