# Quick Start Guide

Get up and running with Mail Monitor in 5 minutes!

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) installed
- Email account with IMAP or POP3 access
- (Optional) Docker for containerized deployment

## Step 1: Configure Your Mailbox

Edit `appsettings.json`. The configuration supports **inheritance** - define shared settings once at the `MailboxSettings` level:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=MailUptime.db"
  },
  "MailboxSettings": {
    "Protocol": "Imap",
    "Host": "imap.gmail.com",
    "Port": 993,
    "UseSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "PollingFrequencySeconds": 60,
    "ExpectedSenderEmails": ["alerts@example.com"],
    "ReportConfig": [
      {
        "Name": "my-first-mailbox"
      }
    ]
  }
}
```

### Gmail Users
1. Enable 2FA on your Google account
2. Generate an App Password: https://myaccount.google.com/apppasswords
3. Use the 16-character app password in the configuration

## Step 2: Run the Application

```bash
cd MailUptime
dotnet run
```

You should see:
```
info: MailUptime.Services.MailUptimeBackgroundService[0]
      Mail Monitor Background Service is starting
info: MailUptime.Services.MailUptimeService[0]
      Starting monitoring for mailbox: my-first-mailbox
```

## Step 3: Test the Endpoint

Open a new terminal and test the endpoint:

```bash
# Check if mail received today
curl http://localhost:5000/api/MailUptime/received-today/my-first-mailbox

# Get detailed status
curl http://localhost:5000/api/MailUptime/status/my-first-mailbox
```

### Expected Responses

**Mail received today:**
```json
{
  "message": "Mail received today",
  "lastChecked": "2025-11-28T12:00:00Z",
  "lastReceivedDate": "2025-11-28T10:30:00Z"
}
```

**No mail yet:**
```json
{
  "message": "No mail received today"
}
```
Status code: `503 Service Unavailable`

## Step 4: Configure Uptime Kuma

In Uptime Kuma:

1. Add new monitor
2. Monitor Type: **HTTP(s)**
3. Friendly Name: `Mail Monitor - my-first-mailbox`
4. URL: `http://your-server:5000/api/MailUptime/received-today/my-first-mailbox`
5. Method: **GET**
6. Expected Status Code: **200**
7. Heartbeat Interval: **60 seconds** (or more)
8. Save

## Common Scenarios

### Scenario 1: Monitor Daily Backup Emails

With inheritance, you can monitor multiple reports from the same mailbox:

```json
{
  "MailboxSettings": {
    "Host": "imap.gmail.com",
    "Port": 993,
    "Username": "your-email@gmail.com",
    "Password": "your-password",
    "ReportConfig": [
      {
        "Name": "daily-backup",
        "ExpectedSenderEmails": ["backup@company.com"],
        "ExpectedSubjectPattern": ".*Backup.*Complete.*",
        "ExpectedBodyPattern": ".*Success.*",
        "PollingFrequencySeconds": 300
      }
    ]
  }
}
```

Test: `curl http://localhost:5000/api/MailUptime/pattern-matched/daily-backup`

### Scenario 2: Monitor Any Email from Specific Sender

```json
{
  "MailboxSettings": {
    "Host": "imap.gmail.com",
    "Username": "your-email@gmail.com",
    "Password": "your-password",
    "ReportConfig": [
      {
        "Name": "alerts-monitor",
        "ExpectedSenderEmails": ["alerts@system.com", "monitoring@system.com"],
        "PollingFrequencySeconds": 180
      }
    ]
  }
}
```

Test: `curl http://localhost:5000/api/MailUptime/received-today/alerts-monitor`

### Scenario 3: Multiple Reports from Same Mailbox

Monitor different types of emails from the same account:

```json
{
  "MailboxSettings": {
    "Host": "imap.gmail.com",
    "Username": "your-email@gmail.com",
    "Password": "your-password",
    "PollingFrequencySeconds": 60,
    "ReportConfig": [
      {
        "Name": "backup-report",
        "ExpectedSubjectPattern": ".*Backup.*"
      },
      {
        "Name": "sales-report",
        "ExpectedSubjectPattern": ".*Sales.*"
      },
      {
        "Name": "system-alerts",
        "ExpectedSubjectPattern": ".*Alert.*",
        "PollingFrequencySeconds": 30
      }
    ]
  }
}
```

## Troubleshooting

### Problem: Authentication Failed

**Solution:**
- Gmail: Use App Password, not regular password
- Outlook: Ensure "Less secure app access" is enabled or use modern auth
- Check username is correct (often the full email address)

### Problem: Connection Timeout

**Solution:**
- Verify Host and Port are correct
- Check firewall allows outbound connections
- Ensure UseSsl matches server requirements (usually `true`)

### Problem: "Mailbox not found or not yet checked"

**Solution:**
- Wait for first polling cycle (60 seconds by default)
- Check mailbox Name in URL matches configuration
- Review application logs for errors

### Problem: Pattern Not Matching

**Solution:**
- Test regex at https://regex101.com
- Remember patterns are case-insensitive by default
- Use `.*` to match any characters
- Check both subject AND body patterns must match (if both configured)

## Next Steps

✅ **Production Deployment**: See [README.md](README.md) for Docker and systemd setup
✅ **Advanced Configuration**: See [CONFIGURATION.md](CONFIGURATION.md) for all options
✅ **Multiple Mailboxes**: Add more entries to the `ReportConfig` array
✅ **Database Queries**: SQLite database at `MailUptime.db` can be queried directly

## Support

- Full documentation: [README.md](README.md)
- Configuration guide: [CONFIGURATION.md](CONFIGURATION.md)
- Project details: [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md)

---

**Happy Monitoring! 📧✅**
