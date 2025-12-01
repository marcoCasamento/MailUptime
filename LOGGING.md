# Logging Guide

This document describes the logging strategy implemented in the Mail Monitor application.

## Logging Levels

The application uses structured logging with the following levels:

| Level | Usage | Examples |
|-------|-------|----------|
| **Trace** | Detailed flow tracking, very verbose | Connection steps, pattern checks, message iteration |
| **Debug** | Diagnostic information for troubleshooting | Message counts, configuration resolution, detailed status |
| **Information** | General application flow | Service startup, check results, API calls |
| **Warning** | Unexpected conditions that don't prevent operation | Pattern not matched, failure detected, missing records |
| **Error** | Failures that need attention | Connection failures, authentication errors |
| **Critical** | System failures that require immediate attention | Background service fatal errors |

## Log Scopes

The application uses logging scopes to add contextual information to all log messages within a scope:

```csharp
using var logScope = _logger.BeginScope(new Dictionary<string, object>
{
    ["MailboxName"] = mailboxName,
    ["Operation"] = "MonitorMailbox",
    ["Protocol"] = config.Protocol?.ToString() ?? "Unknown"
});
```

This ensures all logs within the scope include this context, making it easier to filter and correlate logs.

## Configuration

### Production (`appsettings.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "MailUptime.Services": "Information",
      "MailUptime.Controllers": "Information"
    }
  }
}
```

**Recommended for production**: Captures important events without excessive verbosity.

### Development (`appsettings.Development.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information",
      "MailUptime.Services.MailUptimeService": "Debug",
      "MailUptime.Controllers": "Debug"
    }
  }
}
```

**Recommended for development**: More verbose output for troubleshooting.

### Trace Level (Debugging Connection Issues)

For detailed troubleshooting of mail server connections and pattern matching:

```json
{
  "Logging": {
    "LogLevel": {
      "MailUptime.Services.MailUptimeService": "Trace"
    }
  }
}
```

## Key Logged Events

### Service Lifecycle
- Background service start/stop
- Monitoring loop initialization
- Service cancellation

### Mailbox Monitoring
- Check cycle begins/ends
- Connection to mail server (IMAP/POP3)
- Authentication success/failure
- Message counts and filtering
- Pattern matching results (success/failure)
- Database operations

### API Endpoints
- All API calls with endpoint name
- Request parameters (mailbox names)
- Response status codes
- Failure pattern detection alerts

## Structured Logging Fields

The application uses structured logging with semantic field names:

| Field | Description |
|-------|-------------|
| `MailboxName` | Name of the mailbox being monitored |
| `Operation` | Name of the operation being performed |
| `Endpoint` | API endpoint being called |
| `Protocol` | Mail protocol (Imap/Pop3) |
| `PollingFrequency` | Polling interval in seconds |
| `MessageCount` | Number of messages found |
| `Host` | Mail server hostname |
| `Port` | Mail server port |
| `ReceivedToday` | Whether mail was received today |
| `PatternMatched` | Whether success pattern matched |
| `FailPatternMatched` | Whether failure pattern matched |

## Querying Logs

### Filter by Mailbox
```
MailboxName = "backup-monitor"
```

### Filter by Operation
```
Operation = "MonitorMailbox"
```

### Find Failures
```
FailPatternMatched = true
Level = "Warning"
```

### API Activity
```
Endpoint = "CheckReceivedToday"
```

## Log Examples

### Successful Check
```
[Information] Starting monitoring loop for mailbox: backup-monitor with 300s interval
[Debug] Performing mailbox check for backup-monitor
[Trace] Connecting to IMAP server imap.gmail.com:993 for mailbox backup-monitor
[Information] Found 3 messages from expected senders for backup-monitor
[Information] Success pattern matched in message from backup-monitor. Subject: Daily Backup Successful
[Information] Check completed for backup-monitor: ReceivedToday=True, PatternMatched=True, FailPatternMatched=False
```

### Failure Detection
```
[Information] Starting monitoring loop for mailbox: backup-monitor with 300s interval
[Debug] Performing mailbox check for backup-monitor
[Information] Found 2 messages from expected senders for backup-monitor
[Warning] FAILURE pattern matched in message from backup-monitor. Subject: Backup Failed - Error Code 500
[Information] Check completed for backup-monitor: ReceivedToday=True, PatternMatched=False, FailPatternMatched=True
```

### Connection Error
```
[Error] IMAP connection/check failed for backup-monitor at imap.gmail.com:993
System.Net.Sockets.SocketException: Connection refused
```

## Best Practices

1. **Use appropriate log levels**: Don't log routine operations at Warning or Error
2. **Include context**: Use log scopes to add contextual information
3. **Structure your logs**: Use semantic field names for better querying
4. **Avoid sensitive data**: Never log passwords or email content
5. **Be consistent**: Use the same field names across the application

## Integration with Monitoring Tools

The structured logging format is compatible with:
- **Application Insights**: Azure's monitoring solution
- **Seq**: Structured log server
- **ELK Stack**: Elasticsearch, Logstash, Kibana
- **Serilog**: Popular .NET logging library

Example configuration for Serilog (install `Serilog.AspNetCore`):

```csharp
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
```

## Troubleshooting Guide

### Issue: Too many logs
**Solution**: Increase log level to Warning or Error in production

### Issue: Missing diagnostic information
**Solution**: Lower log level to Debug or Trace for specific namespaces

### Issue: Can't find specific mailbox logs
**Solution**: Use scope filtering with `MailboxName` field

### Issue: Need to debug pattern matching
**Solution**: Set `MailUptime.Services.MailUptimeService` to Trace level

## Performance Considerations

- **Trace level** can impact performance in high-traffic scenarios
- **Debug level** is suitable for most troubleshooting
- **Information level** is recommended for production
- Log scopes are efficient and don't significantly impact performance
