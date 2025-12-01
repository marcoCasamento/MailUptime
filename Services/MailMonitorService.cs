using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Search;
using MailUptime.Data;
using MailUptime.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MailUptime.Services;

public class MailUptimeService : IMailUptimeService
{
    private readonly MailboxSettings _settings;
    private readonly ILogger<MailUptimeService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MailUptimeService(IOptions<MailboxSettings> settings, ILogger<MailUptimeService> logger, IServiceProvider serviceProvider)
    {
        _settings = settings.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public MailCheckResult GetMailStatus(string mailboxName)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MailboxName"] = mailboxName,
            ["Operation"] = "GetMailStatus"
        });

        _logger.LogDebug("Retrieving mail status for mailbox: {MailboxName}", mailboxName);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MailUptimeContext>();
        
        var today = DateTime.Today;
        var record = context.MailCheckRecords
            .FirstOrDefault(r => r.MailboxIdentifier.ToLower() == mailboxName.ToLower() && r.Day == today);

        if (record == null)
        {
            _logger.LogWarning("No record found for mailbox: {MailboxName} on {Date}", mailboxName, today);
            return new MailCheckResult
            {
                Error = "Mailbox not found or not yet checked today"
            };
        }

        _logger.LogDebug("Mail status retrieved for {MailboxName}: PatternMatched={PatternMatched}, FailPatternMatched={FailPatternMatched}", 
            mailboxName, record.PatternMatched, record.FailPatternMatched);

        return new MailCheckResult
        {
            PatternMatched = record.PatternMatched,
            FailPatternMatched = record.FailPatternMatched,
            LastChecked = record.LastCheckTime,
            LastReceivedDate = record.LastReceivedTime,
            LastMatchedSubject = record.LastMatchedSubject,
            LastFailedSubject = record.LastFailedSubject
        };
    }

    public IEnumerable<string> GetAllMailboxNames()
    {
        return _settings.ReportConfig.Select(m => m.Name);
    }

    public IEnumerable<MailboxStatusDto> GetAllMailboxStatuses()
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "GetAllMailboxStatuses"
        });

        _logger.LogDebug("Retrieving status for all configured mailboxes. Count: {MailboxCount}", _settings.ReportConfig.Count);

        var statuses = _settings.ReportConfig.Select(mailbox =>
        {
            var effectiveConfig = mailbox.GetEffectiveConfiguration(_settings);
            var status = GetMailStatus(mailbox.Name);
            var hasPatternConfig = !string.IsNullOrEmpty(effectiveConfig.ExpectedSubjectPattern) || 
                                   !string.IsNullOrEmpty(effectiveConfig.ExpectedBodyPattern);
            var hasFailPatternConfig = !string.IsNullOrEmpty(effectiveConfig.FailSubjectPattern) || 
                                        !string.IsNullOrEmpty(effectiveConfig.FailBodyPattern);
            var hasSenderConfig = effectiveConfig.ExpectedSenderEmails?.Any() ?? false;

            return new MailboxStatusDto
            {
                Name = effectiveConfig.Name,
                PatternMatched = status.PatternMatched,
                FailPatternMatched = status.FailPatternMatched,
                LastChecked = status.LastChecked,
                LastReceivedDate = status.LastReceivedDate,
                LastMatchedSubject = status.LastMatchedSubject,
                LastFailedSubject = status.LastFailedSubject,
                Error = status.Error,
                HasPatternConfiguration = hasPatternConfig,
                HasFailPatternConfiguration = hasFailPatternConfig,
                HasSenderConfiguration = hasSenderConfig,
                ExpectedSenders = effectiveConfig.ExpectedSenderEmails?.ToList() ?? new()
            };
        }).ToList();

        _logger.LogInformation("Retrieved status for {MailboxCount} mailboxes", statuses.Count);
        return statuses;
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "StartMonitoring"
        });

        _logger.LogInformation("Starting monitoring for {MailboxCount} mailboxes", _settings.ReportConfig.Count);

        var tasks = _settings.ReportConfig.Select(mailbox =>
        {
            var effectiveConfig = mailbox.GetEffectiveConfiguration(_settings);
            _logger.LogInformation("Initiating monitoring task for mailbox: {MailboxName}", effectiveConfig.Name);
            return MonitorMailboxAsync(effectiveConfig, cancellationToken);
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("All monitoring tasks have completed");
    }

    private async Task MonitorMailboxAsync(MailboxConfiguration config, CancellationToken cancellationToken)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MailboxName"] = config.Name,
            ["Operation"] = "MonitorMailbox",
            ["Protocol"] = config.Protocol?.ToString() ?? "Unknown",
            ["PollingFrequency"] = config.PollingFrequencySeconds ?? 60
        });

        _logger.LogInformation("Starting monitoring loop for mailbox: {MailboxName} with {PollingFrequency}s interval", 
            config.Name, config.PollingFrequencySeconds ?? 60);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogTrace("Beginning check cycle for mailbox: {MailboxName}", config.Name);

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MailUptimeContext>();
                
                var today = DateTime.Today;
                var record = await context.MailCheckRecords
                    .FirstOrDefaultAsync(r => r.MailboxIdentifier == config.Name && r.Day == today, cancellationToken);

                if (record != null && record.PatternMatched)
                {
                    _logger.LogDebug("Mailbox {MailboxName}: Expected mail already arrived today, skipping check until next interval", config.Name);
                    await Task.Delay(TimeSpan.FromSeconds(config.PollingFrequencySeconds ?? 60), cancellationToken);
                    continue;
                }

                _logger.LogDebug("Performing mailbox check for {MailboxName}", config.Name);
                var result = await CheckMailboxAsync(config, cancellationToken);
                
                if (record == null)
                {
                    _logger.LogDebug("Creating new check record for {MailboxName} on {Date}", config.Name, today);
                    record = new MailCheckRecord
                    {
                        MailboxIdentifier = config.Name,
                        Day = today
                    };
                    context.MailCheckRecords.Add(record);
                }

                record.PatternMatched = result.PatternMatched;
                record.FailPatternMatched = result.FailPatternMatched;
                record.LastCheckTime = DateTime.UtcNow;
                record.LastReceivedTime = result.LastReceivedDate;
                record.LastMatchedSubject = result.LastMatchedSubject;
                record.LastFailedSubject = result.LastFailedSubject;

                await context.SaveChangesAsync(cancellationToken);

                if (result.FailPatternMatched)
                {
                    _logger.LogWarning("Mailbox {MailboxName}: FAILURE PATTERN DETECTED - Subject: {Subject}", 
                        config.Name, result.LastFailedSubject);
                }

                _logger.LogInformation("Check completed for {MailboxName}: PatternMatched={PatternMatched}, FailPatternMatched={FailPatternMatched}",
                    config.Name, record.PatternMatched, record.FailPatternMatched);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Monitoring cancelled for mailbox: {MailboxName}", config.Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking mailbox {MailboxName}. Will retry after delay.", config.Name);
            }

            _logger.LogTrace("Waiting {PollingFrequency}s before next check of {MailboxName}", 
                config.PollingFrequencySeconds ?? 60, config.Name);
            await Task.Delay(TimeSpan.FromSeconds(config.PollingFrequencySeconds ?? 60), cancellationToken);
        }

        _logger.LogInformation("Monitoring loop ended for mailbox: {MailboxName}", config.Name);
    }

    private async Task<MailCheckResult> CheckMailboxAsync(MailboxConfiguration config, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking mailbox {MailboxName} using {Protocol} protocol", config.Name, config.Protocol);

        var result = new MailCheckResult
        {
            LastChecked = DateTime.UtcNow
        };

        try
        {
            if (config.Protocol == MailProtocol.Imap)
            {
                await CheckImapMailboxAsync(config, result, cancellationToken);
            }
            else
            {
                await CheckPop3MailboxAsync(config, result, cancellationToken);
            }

            _logger.LogDebug("Mailbox check completed for {MailboxName}", config.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check mailbox {MailboxName} via {Protocol}", config.Name, config.Protocol);
            throw;
        }

        return result;
    }

    private async Task CheckImapMailboxAsync(MailboxConfiguration config, MailCheckResult result, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Connecting to IMAP server {Host}:{Port} for mailbox {MailboxName}", 
            config.Host, config.Port ?? 993, config.Name);

        using var client = new ImapClient();
        
        try
        {
            await client.ConnectAsync(config.Host ?? string.Empty, config.Port ?? 993, config.UseSsl ?? true, cancellationToken);
            _logger.LogTrace("Connected to IMAP server, authenticating user {Username}", config.Username);
            
            await client.AuthenticateAsync(config.Username ?? string.Empty, config.Password ?? string.Empty, cancellationToken);
            _logger.LogTrace("Authenticated successfully, accessing inbox");

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            _logger.LogDebug("Inbox opened for {MailboxName}, {MessageCount} total messages", config.Name, inbox.Count);

            var today = DateTime.Today;
            SearchQuery query = SearchQuery.DeliveredAfter(today);

            if (config.ExpectedSenderEmails?.Any() ?? false)
            {
                _logger.LogDebug("Filtering by expected senders: {Senders}", string.Join(", ", config.ExpectedSenderEmails));
                var senderQueries = config.ExpectedSenderEmails.Select(email => SearchQuery.FromContains(email));
                SearchQuery? combinedSenderQuery = null;
                foreach (var sq in senderQueries)
                {
                    combinedSenderQuery = combinedSenderQuery == null ? sq : combinedSenderQuery.Or(sq);
                }
                if (combinedSenderQuery != null)
                {
                    query = query.And(combinedSenderQuery);
                }
            }

            var uids = await inbox.SearchAsync(query, cancellationToken);
            _logger.LogDebug("IMAP search returned {MessageCount} messages for {MailboxName}", uids.Count, config.Name);

            if (uids.Count > 0)
            {
                _logger.LogInformation("Found {MessageCount} messages from expected senders for {MailboxName}", uids.Count, config.Name);

                if (!string.IsNullOrEmpty(config.ExpectedSubjectPattern) || !string.IsNullOrEmpty(config.ExpectedBodyPattern))
                {
                    _logger.LogDebug("Checking messages against success patterns for {MailboxName}", config.Name);
                    foreach (var uid in uids)
                    {
                        var message = await inbox.GetMessageAsync(uid, cancellationToken);

                        if (CheckMessagePattern(message, config))
                        {
                            result.PatternMatched = true;
                            result.LastMatchedSubject = message.Subject;
                            result.LastReceivedDate = message.Date.UtcDateTime;
                            _logger.LogInformation("Success pattern matched in message from {MailboxName}. Subject: {Subject}", 
                                config.Name, message.Subject);
                            
                            // Check for fail patterns only on messages that matched success patterns
                            if (!string.IsNullOrEmpty(config.FailSubjectPattern) || !string.IsNullOrEmpty(config.FailBodyPattern))
                            {
                                _logger.LogDebug("Checking matched message against failure patterns for {MailboxName}", config.Name);
                                if (CheckFailPattern(message, config))
                                {
                                    result.FailPatternMatched = true;
                                    result.LastFailedSubject = message.Subject;
                                    _logger.LogWarning("FAILURE pattern matched in success message from {MailboxName}. Subject: {Subject}", 
                                        config.Name, message.Subject);
                                }
                            }
                            
                            break;
                        }
                    }

                    if (!result.PatternMatched)
                    {
                        _logger.LogWarning("No messages matched success pattern for {MailboxName}", config.Name);
                    }

                    if (result.PatternMatched && result.LastReceivedDate == null && uids.Count > 0)
                    {
                        var latestMessage = await inbox.GetMessageAsync(uids[^1], cancellationToken);
                        result.LastReceivedDate = latestMessage.Date.UtcDateTime;
                    }
                }
                else
                {
                    result.PatternMatched = true;
                    var latestMessage = await inbox.GetMessageAsync(uids[^1], cancellationToken);
                    result.LastMatchedSubject = latestMessage.Subject;
                    result.LastReceivedDate = latestMessage.Date.UtcDateTime;
                    _logger.LogDebug("No success pattern configured, marking as matched for {MailboxName}", config.Name);
                    
                    // Check for fail patterns only when no success pattern is configured
                    if (!string.IsNullOrEmpty(config.FailSubjectPattern) || !string.IsNullOrEmpty(config.FailBodyPattern))
                    {
                        _logger.LogDebug("Checking message against failure patterns for {MailboxName}", config.Name);
                        if (CheckFailPattern(latestMessage, config))
                        {
                            result.FailPatternMatched = true;
                            result.LastFailedSubject = latestMessage.Subject;
                            _logger.LogWarning("FAILURE pattern matched in message from {MailboxName}. Subject: {Subject}", 
                                config.Name, latestMessage.Subject);
                        }
                    }
                }
            }
            else
            {
                _logger.LogDebug("No messages found for {MailboxName} today", config.Name);
            }

            await client.DisconnectAsync(true, cancellationToken);
            _logger.LogTrace("Disconnected from IMAP server for {MailboxName}", config.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IMAP connection/check failed for {MailboxName} at {Host}:{Port}", 
                config.Name, config.Host, config.Port ?? 993);
            throw;
        }
    }

    private async Task CheckPop3MailboxAsync(MailboxConfiguration config, MailCheckResult result, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Connecting to POP3 server {Host}:{Port} for mailbox {MailboxName}", 
            config.Host, config.Port ?? 995, config.Name);

        using var client = new Pop3Client();
        
        try
        {
            await client.ConnectAsync(config.Host ?? string.Empty, config.Port ?? 995, config.UseSsl ?? true, cancellationToken);
            _logger.LogTrace("Connected to POP3 server, authenticating user {Username}", config.Username);
            
            await client.AuthenticateAsync(config.Username ?? string.Empty, config.Password ?? string.Empty, cancellationToken);
            _logger.LogTrace("Authenticated successfully");

            var messageCount = await client.GetMessageCountAsync(cancellationToken);
            _logger.LogDebug("POP3 mailbox {MailboxName} has {MessageCount} total messages", config.Name, messageCount);
            
            var today = DateTime.Today;
            var todayMessageCount = 0;

            for (int i = messageCount - 1; i >= 0; i--)
            {
                var message = await client.GetMessageAsync(i, cancellationToken);

                if (message.Date.Date < today)
                {
                    _logger.LogTrace("Reached messages older than today, stopping POP3 scan");
                    break;
                }

                todayMessageCount++;

                if (config.ExpectedSenderEmails?.Any() ?? false)
                {
                    var messageFrom = message.From.ToString();
                    if (!config.ExpectedSenderEmails.Any(email => messageFrom.Contains(email, StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogTrace("Message from {From} does not match expected senders, skipping", messageFrom);
                        continue;
                    }
                }

                if (CheckMessagePattern(message, config))
                {
                    result.PatternMatched = true;
                    result.LastMatchedSubject = message.Subject;
                    result.LastReceivedDate = message.Date.UtcDateTime;
                    _logger.LogInformation("Success pattern matched in POP3 message from {MailboxName}. Subject: {Subject}", 
                        config.Name, message.Subject);
                    
                    // Check for fail patterns only on messages that matched success patterns
                    if (!string.IsNullOrEmpty(config.FailSubjectPattern) || !string.IsNullOrEmpty(config.FailBodyPattern))
                    {
                        _logger.LogDebug("Checking matched message against failure patterns for {MailboxName}", config.Name);
                        if (CheckFailPattern(message, config))
                        {
                            result.FailPatternMatched = true;
                            result.LastFailedSubject = message.Subject;
                            _logger.LogWarning("FAILURE pattern matched in success message from {MailboxName}. Subject: {Subject}", 
                                config.Name, message.Subject);
                        }
                    }
                }
                else if (result.LastReceivedDate == null)
                {
                    result.LastReceivedDate = message.Date.UtcDateTime;
                }
            }

            _logger.LogDebug("Processed {TodayMessageCount} messages from today for {MailboxName}", todayMessageCount, config.Name);

            await client.DisconnectAsync(true, cancellationToken);
            _logger.LogTrace("Disconnected from POP3 server for {MailboxName}", config.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POP3 connection/check failed for {MailboxName} at {Host}:{Port}", 
                config.Name, config.Host, config.Port ?? 995);
            throw;
        }
    }

    private bool CheckMessagePattern(MimeKit.MimeMessage message, MailboxConfiguration config)
    {
        bool subjectMatch = true;
        bool bodyMatch = true;

        if (!string.IsNullOrEmpty(config.ExpectedSubjectPattern))
        {
            subjectMatch = Regex.IsMatch(message.Subject ?? string.Empty, config.ExpectedSubjectPattern, RegexOptions.IgnoreCase);
            _logger.LogTrace("Success subject pattern check: {Match} for subject '{Subject}'", subjectMatch, message.Subject);
        }

        if (!string.IsNullOrEmpty(config.ExpectedBodyPattern))
        {
            var bodyText = message.TextBody ?? message.HtmlBody ?? string.Empty;
            bodyMatch = Regex.IsMatch(bodyText, config.ExpectedBodyPattern, RegexOptions.IgnoreCase);
            _logger.LogTrace("Success body pattern check: {Match}", bodyMatch);
        }

        var result = subjectMatch && bodyMatch;
        _logger.LogTrace("Overall success pattern match result: {Match}", result);
        return result;
    }

    private bool CheckFailPattern(MimeKit.MimeMessage message, MailboxConfiguration config)
    {
        bool subjectMatch = false;
        bool bodyMatch = false;

        if (!string.IsNullOrEmpty(config.FailSubjectPattern))
        {
            subjectMatch = Regex.IsMatch(message.Subject ?? string.Empty, config.FailSubjectPattern, RegexOptions.IgnoreCase);
            _logger.LogTrace("Fail subject pattern check: {Match} for subject '{Subject}'", subjectMatch, message.Subject);
        }

        if (!string.IsNullOrEmpty(config.FailBodyPattern))
        {
            var bodyText = message.TextBody ?? message.HtmlBody ?? string.Empty;
            bodyMatch = Regex.IsMatch(bodyText, config.FailBodyPattern, RegexOptions.IgnoreCase);
            _logger.LogTrace("Fail body pattern check: {Match}", bodyMatch);
        }

        // If neither pattern is configured, return false (no fail detected)
        if (string.IsNullOrEmpty(config.FailSubjectPattern) && string.IsNullOrEmpty(config.FailBodyPattern))
        {
            return false;
        }

        // If only one pattern is configured, return that result
        if (string.IsNullOrEmpty(config.FailSubjectPattern))
        {
            _logger.LogTrace("Only fail body pattern configured, result: {Match}", bodyMatch);
            return bodyMatch;
        }
        if (string.IsNullOrEmpty(config.FailBodyPattern))
        {
            _logger.LogTrace("Only fail subject pattern configured, result: {Match}", subjectMatch);
            return subjectMatch;
        }

        // If both are configured, both must match for a fail
        var result = subjectMatch && bodyMatch;
        _logger.LogTrace("Overall fail pattern match result: {Match}", result);
        return result;
    }
}
