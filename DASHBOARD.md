# Dashboard Status Indicators

This document explains the color-coded status badges shown on the MailUptime Dashboard.

## Status Badge Colors

The dashboard displays a status badge for each configured report with the following logic:

### 🟢 Green Badge - "✅ Received"
**Indicates: Report received successfully**

Conditions:
- Mail from expected sender was received today
- No failure patterns were detected (or no failure patterns configured)

This is the desired state - your report arrived and contains no errors.

### 🟡 Yellow Badge - "⏳ Pending"
**Indicates: Waiting for today's report**

Conditions:
- No mail received yet today from expected senders

This is normal during the day before the report is sent. Check the "Last Received" timestamp to see when the last successful report arrived.

### 🔴 Red Badge - "❌ Failed"
**Indicates: Report received but contains failure indicators**

Conditions:
- Mail from expected sender was received today
- The email **first matched** the success patterns (`ExpectedSubjectPattern`/`ExpectedBodyPattern`)
- The **same email** then matched the configured `FailSubjectPattern` and/or `FailBodyPattern`

This requires attention - the report was received in the expected format but contains failure or error indicators within the content.

### 🔴 Red Badge - "Error"
**Indicates: System error checking the mailbox**

Conditions:
- Unable to check the mailbox (connection error, authentication failure, etc.)
- See the error message displayed on the card for details

## Dashboard Elements

### For Each Report Card

#### Status Information
- **Last Check**: When the mailbox was last polled
- **Last Received**: When the most recent email was received
- **Last Subject**: Subject line of the last matched email
- **?? Failed Report Subject**: Subject line when failure detected (only shown when red badge)

#### Check Items
Each card shows status indicators for:
- ✅ **Report Received**: Email matching the expected patterns arrived today
- ⏳ **Report not yet received**: No matching email has arrived today
- ❌ **Failing Report Received**: Email received but contains failure patterns (only shown when failures detected)

#### Warning Box
When a failure is detected, a prominent warning box appears:
```
⚠️ Failure Detected: This report contains failure patterns 
indicating an issue that needs attention.
```

## Configuration Examples

### Example 1: Backup Report Monitoring

```json
{
  "Name": "Daily Backup",
  "ExpectedSubjectPattern": ".*Backup.*",
  "FailSubjectPattern": ".*failed.*",
  "FailBodyPattern": ".*error.*"
}
```

**Dashboard Behavior:**
- 🟢 **Green**: Email with "Backup" in subject received, no "failed" or "error" found
- 🔴 **Red**: Email with "Backup" in subject received AND contains "failed" in subject AND "error" in body
- 🟡 **Yellow**: No backup email received yet today

**Important**: The fail patterns are only checked on emails that already matched "Backup" in the subject. Emails without "Backup" are ignored entirely.

### Example 2: Report Delivery (No Fail Patterns)

```json
{
  "Name": "Sales Report",
  "ExpectedSubjectPattern": ".*Sales Report.*"
}
```

**Dashboard Behavior:**
- 🟢 **Green**: Email with "Sales Report" in subject received
- 🟡 **Yellow**: No report email received yet today
- Red badge never shown (no fail patterns configured)

### Example 3: Simple Email Receipt Check

```json
{
  "Name": "System Notifications",
  "ExpectedSenderEmails": ["alerts@system.com"]
}
```

**Dashboard Behavior:**
- 🟢 **Green**: Any email from alerts@system.com received today
- 🟡 **Yellow**: No email from alerts@system.com yet today
- Red badge never shown (no fail patterns configured)

## Monitoring Strategy

### Using the Dashboard

1. **Morning Check**: All reports should show:
- 🟡 Yellow badge (pending)
- ⏳ "Report not yet received"

2. **Throughout the Day**: As reports arrive, cards update to show:
- 🟢 Green badge (received)
- ✅ "Report Received"

3. **Issues Detected**: When failures are detected, cards show:
- 🔴 Red badge (failed)
- ✅ "Report Received" 
- ❌ "Failing Report Received" (highlighted in red)
- Check the "Failed Report Subject" for details
- Review the actual email for complete error information

### Integration with Uptime Kuma

For automated monitoring, use the API endpoints:

```bash
# Returns 200 if mail received (green badge scenario)
GET /api/MailUptime/received-today/{reportName}

# Returns 503 if failure detected (red badge scenario)
GET /api/MailUptime/fail-pattern-matched/{reportName}
```

This allows external monitoring tools to alert you when:
- Reports don't arrive on schedule
- Failure patterns are detected

## Auto-Refresh

The dashboard automatically refreshes every 30 seconds to show the latest status. You can also manually refresh using the "Refresh" button.

## Troubleshooting

### Badge shows Yellow but report was sent
- Check the "Expected Senders" configuration matches the actual sender
- Verify the `ExpectedSubjectPattern` matches the actual subject
- Review logs for pattern matching details

### Badge shows Red for normal reports
- Review the `FailSubjectPattern` and `FailBodyPattern` configuration
- The patterns may be too broad and matching normal content
- Test patterns at https://regex101.com

### Badge shows Error
- Check mail server connectivity
- Verify credentials in configuration
- Review application logs for detailed error messages

## Mobile Support

The dashboard is fully responsive and works on:
- Desktop browsers
- Tablets
- Mobile phones

All status indicators are clearly visible on small screens.
