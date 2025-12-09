# Docker Compose Quick Reference

## Setup Commands

```bash
# 1. Edit configuration
nano config/appsettings.json

# 2. Start service
docker-compose up -d

# 3. View logs
docker-compose logs -f

# 4. Check status
docker-compose ps

# 5. Test API
curl http://localhost:5000/api/dashboard/mailboxes
```

## Directory Structure

```
MailUptime/
??? docker-compose.yml          # Main compose file
??? config/
?   ??? appsettings.json       # Your email configuration (EDIT THIS)
??? data/
    ??? MailUptime.db          # SQLite database (auto-created)
```

## Access Points

- **Web Dashboard**: http://localhost:5000/
- **API**: http://localhost:5000/api/dashboard/mailboxes
- **Health Check**: http://localhost:5000/api/MailUptime/received-today/[mailbox-name]

## Configuration Template

Edit `config/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/app/data/MailUptime.db"
  },
  "MailboxSettings": {
    "Protocol": "Imap",
    "Host": "imap.gmail.com",
    "Port": 993,
    "UseSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "PollingFrequencySeconds": 60,
    "ExpectedSenderEmails": ["sender@example.com"],
    "ReportConfig": [
      {
        "Name": "my-mailbox",
        "ExpectedSubjectPattern": ".*Report.*"
      }
    ]
  }
}
```

## Common Tasks

### Restart after config change
```bash
docker-compose restart
```

### Update to latest version
```bash
docker-compose pull
docker-compose up -d
```

### Backup database
```bash
docker-compose stop
cp data/MailUptime.db data/backup-$(date +%Y%m%d).db
docker-compose start
```

### View container details
```bash
docker-compose logs mailuptime
docker inspect mailuptime
```

### Stop and remove
```bash
docker-compose down  # Keeps data and config
```

## Troubleshooting

### Container not starting?
```bash
docker-compose logs mailuptime
```

### Can't connect to mailbox?
- Check `config/appsettings.json` credentials
- Verify Host, Port, and UseSsl settings
- Test from host: `telnet imap.gmail.com 993`

### Database locked?
```bash
docker-compose down
docker-compose up -d
```

### Port 5000 already in use?
Edit `docker-compose.yml` and change:
```yaml
ports:
  - "8080:8080"  # Use different external port
```

## Security Checklist

- [ ] Change default credentials in `config/appsettings.json`
- [ ] Use app-specific passwords (not main account password)
- [ ] Set file permissions: `chmod 600 config/appsettings.json`
- [ ] Never commit `config/appsettings.json` to git
- [ ] Regular backups of `data/` directory
- [ ] Keep Docker image updated: `docker-compose pull`

## Monitoring Integration

### Uptime Kuma
1. Add HTTP(s) monitor
2. URL: `http://localhost:5000/api/MailUptime/received-today/my-mailbox`
3. Method: GET
4. Expected Status: 200
5. Interval: 60 seconds

### Prometheus
Endpoint: `http://localhost:5000/api/MailUptime/status/my-mailbox`

## Full Documentation

- **Complete Docker Compose Guide**: [DOCKER_COMPOSE.md](DOCKER_COMPOSE.md)
- **Configuration Reference**: [CONFIGURATION.md](CONFIGURATION.md)
- **Quick Start**: [QUICKSTART.md](QUICKSTART.md)
- **Main README**: [README.md](README.md)
