# Mail Monitor

[![Build Status](https://github.com/marcoCasamento/MailUptime/actions/workflows/build.yml/badge.svg)](https://github.com/marcoCasamento/MailUptime/actions/workflows/build.yml)
[![Docker Build](https://github.com/marcoCasamento/MailUptime/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/marcoCasamento/MailUptime/actions/workflows/docker-publish.yml)

A .NET 10 application that monitors mailboxes (IMAP/POP3) and provides HTTP endpoints to check mail delivery status.

## Features

- **Beautiful Web Dashboard** - Mobile-friendly, real-time status overview with auto-refresh
- Monitor multiple mailboxes simultaneously (IMAP or POP3)
- Configurable polling frequency per mailbox
- Check if mail was received today from specific senders
- Pattern matching with regex on subject and body
- SQLite database for persistent storage of mail check results
- Automatic stop polling when expected mail arrives for the day
- REST API endpoints for monitoring tools like Uptime Kuma
- **Interactive API documentation** with OpenAPI and Scalar UI
- Cross-platform support (Windows, Linux, macOS)

## Configuration

Edit `appsettings.json` to configure your mailboxes. The configuration supports **inheritance** - define shared settings at the `MailboxSettings` level, and override them per report as needed:

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
    "ExpectedSenderEmails": ["noreply@example.com", "alerts@example.com"],
    "ReportConfig": [
      {
        "Name": "my-mailbox",
        "ExpectedSubjectPattern": "Daily Report.*",
        "ExpectedBodyPattern": "Status: OK"
      }
    ]
  }
}
```

### Configuration Properties

#### Inheritable Settings (can be set at MailboxSettings or ReportConfig level)
- **Protocol**: `Imap` or `Pop3` - defaults to Imap
- **Host**: Mail server hostname
- **Port**: Mail server port (defaults: 993 for IMAP SSL, 995 for POP3 SSL)
- **UseSsl**: Enable SSL/TLS connection - defaults to true
- **Username**: Email account username
- **Password**: Email account password
- **PollingFrequencySeconds**: How often to check the mailbox - defaults to 60 seconds
- **ExpectedSenderEmails**: List of sender email addresses to monitor (empty list = accept from any sender)

#### Report-Specific Settings (set at ReportConfig level only)
- **Name**: Unique identifier for the mailbox (required, used in API endpoints and database)
- **ExpectedSubjectPattern**: (Optional) Regex pattern to match in subject for success
- **ExpectedBodyPattern**: (Optional) Regex pattern to match in body for success
- **FailSubjectPattern**: (Optional) Regex pattern to match in subject for failure detection
- **FailBodyPattern**: (Optional) Regex pattern to match in body for failure detection

### Configuration Inheritance

Settings defined at the `MailboxSettings` level are inherited by all reports in `ReportConfig`. Each report can override any inherited setting by specifying it explicitly. This allows you to:
- Share common credentials across multiple reports
- Use the same mail server for different report types
- Override specific settings (like polling frequency) per report

### How It Works

The service continuously polls configured mailboxes according to their polling frequency. For each mailbox:

1. Checks for emails received today from the configured sender list
2. If patterns are configured, verifies they match the email content
3. Stores the result in SQLite database with daily tracking
4. **Stops polling for the day once the expected mail arrives** (optimizes resource usage)
5. Automatically resets at midnight to check for the next day's mail

All check results are stored in `MailUptime.db` with:
- Mailbox identifier
- Date
- Mail arrival status
- Pattern match status
- Last check time
- Last received time

## Web Dashboard

Access the beautiful, mobile-friendly web dashboard at:

**`http://localhost:5000/`**

The dashboard provides a real-time overview of all configured mailboxes with:

- ?? **Visual status cards** for each mailbox
- ? **Check indicators** showing if emails were received from expected senders
- ?? **Pattern match status** for subject/body patterns
- ? **Last check and received timestamps**
- ?? **Auto-refresh every 30 seconds**
- ?? **Fully responsive** mobile-friendly design
- ?? **Dark mode** interface

### Dashboard Features

Each mailbox card displays:
- Current status (OK, Pending, or Error)
- Last polling time (when the mailbox was last checked)
- Whether email from expected sender was received today
- Whether pattern matching (subject/body) succeeded
- List of expected sender emails (if configured)
- Last received email subject line
- Human-readable timestamps (e.g., "5 minutes ago")

## API Endpoints

### Interactive API Documentation

When running in development mode, you can access interactive API documentation:

- **Scalar UI**: `http://localhost:5000/scalar/v1` - Modern, interactive API documentation
- **OpenAPI Spec**: `http://localhost:5000/openapi/v1.json` - Raw OpenAPI specification

The Scalar UI provides:
- Interactive request/response testing
- Code examples in multiple languages
- Detailed endpoint documentation
- Request/response schema visualization

### Get all mailbox statuses

```
GET /api/dashboard/mailboxes
```

Returns status information for all configured mailboxes in JSON format.

**Example:**
```bash
curl http://localhost:5000/api/dashboard/mailboxes
```

### Check if mail received today

```
GET /api/MailUptime/received-today/{mailboxName}
```

Returns `200 OK` if mail matching criteria was received today, otherwise `503 Service Unavailable`.

**Example:**
```bash
curl http://localhost:5000/api/MailUptime/received-today/my-mailbox
```

### Check if pattern matched

```
GET /api/MailUptime/pattern-matched/{mailboxName}
```

Returns `200 OK` if mail matching the regex patterns was received today, otherwise `503 Service Unavailable`.

**Example:**
```bash
curl http://localhost:5000/api/MailUptime/pattern-matched/my-mailbox
```

### Check if failure pattern matched

```
GET /api/MailUptime/fail-pattern-matched/{mailboxName}
```

Returns `503 Service Unavailable` if mail matching the fail patterns was detected (indicating a problem), otherwise `200 OK`.

**Example:**
```bash
curl http://localhost:5000/api/MailUptime/fail-pattern-matched/my-mailbox
```

**Use Case**: Monitor this endpoint with Uptime Kuma to get alerted when failure emails are detected (e.g., "Backup Failed" notifications).

### Get full status

```
GET /api/MailUptime/status/{mailboxName}
```

Returns detailed status information including timestamps and errors.

**Example:**
```bash
curl http://localhost:5000/api/MailUptime/status/my-mailbox
```

## Running the Application

### Development

```bash
dotnet run
```

### Production (Linux)

```bash
dotnet publish -c Release -o ./publish
cd publish
./MailUptime
```

### Docker

#### Using Pre-built Image from Docker Hub

```bash
# Pull and run latest version
docker pull mcasamento/mailuptime:latest
docker run -p 5000:8080 -v $(pwd)/appsettings.json:/app/appsettings.json mcasamento/mailuptime:latest

# Or run specific version
docker run -p 5000:8080 -v $(pwd)/appsettings.json:/app/appsettings.json mcasamento/mailuptime:v1.0.0
```

#### Building from Source

Build and run:

```bash
docker build -t mailuptime .
docker run -p 5000:8080 -v $(pwd)/appsettings.json:/app/appsettings.json mailuptime
```

#### Docker Compose (Recommended)

The easiest way to deploy with externalized configuration and persistent database storage.

**Quick start:**

```bash
# Configuration is in config/appsettings.json
# Database will be stored in data/MailUptime.db
docker-compose up -d
```

**Features:**
- Externalized configuration in `config/appsettings.json`
- Persistent SQLite database in `data/` directory
- Automatic health checks
- Auto-restart on failure

For detailed Docker Compose setup and configuration options, see **[DOCKER_COMPOSE.md](DOCKER_COMPOSE.md)**.

### Systemd Service (Linux)

Create `/etc/systemd/system/MailUptime.service`:

```ini
[Unit]
Description=Mail Monitor Service
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/MailUptime
ExecStart=/usr/bin/dotnet /opt/MailUptime/MailUptime.dll
Restart=always
RestartSec=10
User=MailUptime
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable MailUptime
sudo systemctl start MailUptime
```

## Security Considerations

- Store passwords securely using environment variables or secret management
- Use app-specific passwords for Gmail/Outlook
- Enable SSL/TLS for mail connections
- Restrict API access with authentication middleware if needed
- Run with minimal privileges on Linux

## Uptime Kuma Integration

Add an HTTP(s) monitor in Uptime Kuma:

- **URL**: `http://your-server:5000/api/MailUptime/received-today/my-mailbox`
- **Method**: GET
- **Expected Status Code**: 200

## GitHub Actions & CI/CD

This project includes automated CI/CD workflows:

- **Continuous Integration**: Automatic builds and tests on every push and pull request
- **Docker Publishing**: Multi-architecture Docker images published to Docker Hub on releases
- **Automated Versioning**: Semantic version tags automatically create corresponding Docker image tags

For setup instructions, see [GITHUB_ACTIONS.md](GITHUB_ACTIONS.md).

## License

GNU GPL v3.0
