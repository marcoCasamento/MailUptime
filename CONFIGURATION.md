# Configuration Guide

## Quick Start

1. Copy `appsettings.example.json` to `appsettings.json`
2. Edit the mailbox configurations with your actual credentials
3. Run the application: `dotnet run`

## Configuration Inheritance

The configuration system supports **inheritance** to avoid repetition. You can define default settings at the `MailboxSettings` level, and each report in `ReportConfig` can inherit or override these defaults.

### Example: Shared Settings
```json
{
  "MailboxSettings": {
    "Protocol": "Imap",
    "Host": "imap.gmail.com",
    "Port": 993,
    "UseSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "PollingFrequencySeconds": 300,
    "ExpectedSenderEmails": [],
    "ReportConfig": [
      {
        "Name": "backup-report",
        "ExpectedSubjectPattern": ".*Backup.*",
        "FailSubjectPattern": ".*failed.*",
        "FailBodyPattern": ".*Error.*"
      },
      {
        "Name": "sales-report",
        "ExpectedSubjectPattern": ".*Sales.*",
        "PollingFrequencySeconds": 600
      }
    ]
  }
}
```

In this example:
- `backup-report` inherits all settings from `MailboxSettings` and includes failure patterns to detect errors
- `sales-report` inherits most settings but overrides `PollingFrequencySeconds` to 600 and has no failure patterns configured

## Mailbox Configuration

Each mailbox in the configuration supports the following properties:

### Required Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `Name` | string | Unique identifier for this mailbox | `"gmail-backup"` |
| `Protocol` | string | Mail protocol to use | `"Imap"` or `"Pop3"` |
| `Host` | string | Mail server hostname | `"imap.gmail.com"` |
| `Port` | integer | Mail server port | `993` (IMAP/SSL), `995` (POP3/SSL) |
| `UseSsl` | boolean | Enable SSL/TLS | `true` |
| `Username` | string | Email account username | `"user@example.com"` |
| `Password` | string | Email account password | `"app-password"` |
| `PollingFrequencySeconds` | integer | Check interval in seconds | `300` (5 minutes) |

### Optional Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `ExpectedSenderEmails` | array | List of allowed sender addresses | `["alerts@example.com", "noreply@backup.com"]` |
| `ExpectedSubjectPattern` | string | Regex pattern for successful email subject | `".*Daily Backup Successful.*"` |
| `ExpectedBodyPattern` | string | Regex pattern for successful email body | `".*Status: OK.*"` |
| `FailSubjectPattern` | string | Regex pattern for failure in subject | `".*failed.*"` or `".*error.*"` |
| `FailBodyPattern` | string | Regex pattern for failure in body | `".*Error.*"` or `".*FAILED.*"` |

## Sender Configuration

The `ExpectedSenderEmails` field controls which senders are considered valid:

- **Empty array `[]`**: Accept emails from ANY sender
- **One or more emails**: Only accept emails from these specific senders

### Example: Accept from any sender
```json
{
  "Name": "any-mail-check",
  "ExpectedSenderEmails": []
}
```

### Example: Accept from specific senders
```json
{
  "Name": "backup-check",
  "ExpectedSenderEmails": [
    "backup@company.com",
    "alerts@monitoring.com"
  ]
}
```

## Pattern Matching

Patterns use standard .NET regex syntax:

### Success vs Failure Patterns

The configuration supports two types of pattern matching:

1. **Success Patterns** (`ExpectedSubjectPattern`, `ExpectedBodyPattern`):
   - Used to identify emails that indicate successful operations
   - Both patterns must match for success (if both are configured)
   - If patterns are not configured, any email from expected senders is considered successful

2. **Failure Patterns** (`FailSubjectPattern`, `FailBodyPattern`):
   - Used to identify emails that indicate failed operations or errors
   - **Only checked on emails that already matched the success patterns**
   - Both patterns must match for failure detection (if both are configured)
   - If failure patterns are not configured, no failure detection occurs
   - Useful for monitoring systems that send both success and failure notifications in the same email format

**Example Use Case**: Monitor a backup system that sends emails with "Backup" in the subject. The email may contain either success or failure indicators in the body.

```json
{
  "Name": "backup-monitor",
  "ExpectedSubjectPattern": ".*Backup.*",
  "FailSubjectPattern": ".*failed.*",
  "FailBodyPattern": ".*Error.*"
}
```

In this example:
1. First, the system finds emails with "Backup" in the subject (success pattern)
2. Then, for those matched emails, it checks if they also contain "failed" in subject AND "Error" in body
3. If both fail patterns match, the report is marked as failed
4. Emails without "Backup" in subject are completely ignored, even if they contain "failed" or "Error"

### Subject Pattern Examples

```regex
.*Daily Report.*          # Contains "Daily Report" anywhere
^System Alert:.*          # Starts with "System Alert:"
.*\[SUCCESS\]$            # Ends with "[SUCCESS]"
(Backup|Archive) Complete # Contains "Backup Complete" OR "Archive Complete"
```

### Body Pattern Examples

```regex
.*Status: OK.*            # Contains "Status: OK"
.*files backed up: \d+.*  # Contains "files backed up: " followed by numbers
(?i)success               # Case-insensitive "success"
```

## Database

The application uses SQLite to store check results:

- **Database file**: `MailUptime.db` (created automatically)
- **Location**: Same directory as the application
- **Schema**: Automatically created via Entity Framework migrations

### Database Structure

```
MailCheckRecords:
- Id (Primary Key)
- MailboxIdentifier (string) - matches the "Name" in config
- Day (date) - date of the check
- MailArrived (boolean) - true if mail from expected sender arrived
- PatternMatched (boolean) - true if success patterns matched
- FailPatternMatched (boolean) - true if failure patterns matched
- LastCheckTime (datetime) - when the last check occurred
- LastReceivedTime (datetime) - when the last matching email was received
- LastMatchedSubject (string) - subject of the last matched email
- LastFailedSubject (string) - subject of the email when failure patterns matched
```

## Gmail App Passwords

For Gmail accounts, you must use an **App Password** instead of your regular password:

1. Enable 2-Factor Authentication on your Google account
2. Go to: https://myaccount.google.com/apppasswords
3. Select "Mail" and your device
4. Copy the generated 16-character password
5. Use this password in the configuration

## Microsoft 365 / Outlook

For Microsoft 365 accounts:

- **Host**: `outlook.office365.com`
- **Port**: `993` (IMAP)
- **Username**: Your full email address
- **Password**: Your account password or app password

## Polling Strategy

The service optimizes polling by **stopping checks once the expected mail arrives**:

1. Service starts polling at configured frequency
2. Checks mailbox for today's emails from expected senders
3. If patterns are configured, validates pattern matching
4. Once expected mail is found and patterns match:
   - **Polling stops for the rest of the day**
   - Service waits until next day to resume checking
5. At midnight (UTC), the cycle resets for the new day

This saves resources and reduces unnecessary mail server connections.

## Inheritance Rules

Settings are resolved in this order (later overrides earlier):
1. **Hard-coded defaults** in the code (e.g., Port 993 for IMAP)
2. **MailboxSettings level** - shared defaults for all reports
3. **ReportConfig level** - specific settings for each report

### Inheritable Settings
- `Protocol` - Mail protocol (Imap or Pop3)
- `Host` - Mail server hostname
- `Port` - Mail server port
- `UseSsl` - Enable SSL/TLS
- `Username` - Email account username
- `Password` - Email account password
- `PollingFrequencySeconds` - Check interval
- `ExpectedSenderEmails` - List of expected senders

### Non-Inheritable Settings
- `Name` - Always required and unique per report
- `ExpectedSubjectPattern` - Report-specific success pattern
- `ExpectedBodyPattern` - Report-specific success pattern
- `FailSubjectPattern` - Report-specific failure pattern (optional)
- `FailBodyPattern` - Report-specific failure pattern (optional)

## Environment Variables

You can override configuration using environment variables:

```bash
# Connection string
export ConnectionStrings__DefaultConnection="Data Source=/data/MailUptime.db"

# Shared settings at MailboxSettings level
export MailboxSettings__Host="imap.gmail.com"
export MailboxSettings__Username="your-email@gmail.com"

# Report-specific settings
export MailboxSettings__ReportConfig__0__Name="my-mailbox"
export MailboxSettings__ReportConfig__0__ExpectedSubjectPattern=".*Report.*"
export MailboxSettings__ReportConfig__0__FailSubjectPattern=".*failed.*"
export MailboxSettings__ReportConfig__0__FailBodyPattern=".*Error.*"
# ... etc
```

## Security Best Practices

1. **Never commit `appsettings.json` with real credentials**
2. Use environment variables or secrets management in production
3. Restrict file permissions on `appsettings.json`: `chmod 600 appsettings.json`
4. Use read-only mailbox access if possible
5. Enable SSL/TLS for all connections
6. Use app-specific passwords instead of main account passwords
7. Consider using OAuth2 for Gmail/Microsoft 365 (future enhancement)

## Troubleshooting

### Mail not being detected

1. Check sender email matches exactly (case-insensitive comparison is used)
2. Verify regex patterns are correct - test with https://regex101.com
3. Check mailbox credentials are valid
4. Ensure firewall allows outbound connections to mail server
5. Review logs: `tail -f /var/log/MailUptime/app.log`

### Database locked errors

- Only one instance should access the database
- Ensure previous instance is stopped before starting new one
- Check file permissions on `MailUptime.db`

### SSL/TLS errors on Linux

Install required certificates:
```bash
sudo apt-get install ca-certificates
```
