# Docker Compose Deployment Guide

This guide explains how to deploy MailUptime using Docker Compose with externalized configuration and database storage.

## Directory Structure

The Docker Compose setup uses the following directory structure:

```
MailUptime/
??? docker-compose.yml          # Docker Compose configuration
??? config/                     # Configuration directory
?   ??? appsettings.json       # Application settings (externalized)
??? data/                       # Database directory
    ??? MailUptime.db          # SQLite database (auto-created)
```

## Quick Start

### 1. Clone or Download

If you haven't already, get the files:

```bash
git clone https://github.com/marcoCasamento/MailUptime.git
cd MailUptime
```

Or create the directory structure manually:

```bash
mkdir -p MailUptime/config MailUptime/data
cd MailUptime
```

### 2. Configure Your Mailbox

Edit `config/appsettings.json` with your email credentials:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
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
    "ExpectedSenderEmails": ["alerts@example.com"],
    "ReportConfig": [
      {
        "Name": "my-mailbox",
        "ExpectedSubjectPattern": ".*Daily Report.*",
        "ExpectedBodyPattern": ".*Status: OK.*"
      }
    ]
  }
}
```

**Important Security Notes:**
- Never commit `config/appsettings.json` with real credentials to version control
- Set appropriate file permissions: `chmod 600 config/appsettings.json`
- Consider using environment variables for sensitive data in production

### 3. Start the Service

```bash
docker-compose up -d
```

This will:
- Pull the latest `mcasamento/mailuptime` image from Docker Hub
- Create and start the container
- Mount `config/appsettings.json` as read-only inside the container
- Mount `data/` directory for persistent SQLite database storage
- Expose the service on port 5000

### 4. Verify It's Running

Check container status:

```bash
docker-compose ps
```

View logs:

```bash
docker-compose logs -f
```

Test the API:

```bash
curl http://localhost:5000/api/dashboard/mailboxes
```

Access the web dashboard:

```
http://localhost:5000/
```

## Configuration Details

### Volume Mounts

The `docker-compose.yml` file configures two separate volumes:

1. **Configuration Volume** (`./config/appsettings.json:/app/appsettings.json:ro`)
   - Maps your local `config/appsettings.json` to the container
   - Mounted as **read-only** (`:ro`) for security
   - Changes require container restart to take effect

2. **Database Volume** (`./data:/app/data`)
   - Maps your local `data/` directory to the container's data directory
   - Stores the SQLite database file (`MailUptime.db`)
   - Persists across container restarts and updates
   - **Read-write** access so the app can update the database

### Environment Variables

The following environment variables are set in `docker-compose.yml`:

| Variable | Value | Purpose |
|----------|-------|---------|
| `ConnectionStrings__DefaultConnection` | `Data Source=/app/data/MailUptime.db` | Points database to external volume |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Sets production environment mode |
| `ASPNETCORE_URLS` | `http://+:8080` | Internal port binding (mapped to 5000 externally) |

### Port Mapping

- **External**: Port `5000` (accessible from your host machine)
- **Internal**: Port `8080` (inside the container)
- Format: `"5000:8080"` means external:internal

To change the external port, edit `docker-compose.yml`:

```yaml
ports:
  - "8080:8080"  # Use port 8080 instead
```

## Common Operations

### View Logs

```bash
# Follow logs in real-time
docker-compose logs -f

# View last 50 lines
docker-compose logs --tail=50

# View logs for specific service
docker-compose logs mailuptime
```

### Restart Service

After changing `config/appsettings.json`:

```bash
docker-compose restart
```

### Stop Service

```bash
docker-compose stop
```

### Start Service

```bash
docker-compose start
```

### Stop and Remove Container

```bash
docker-compose down
```

**Note**: This removes the container but preserves your data in `config/` and `data/` directories.

### Update to Latest Version

```bash
docker-compose pull
docker-compose up -d
```

This pulls the latest image and recreates the container. Your configuration and database remain intact.

### Backup Database

```bash
# Stop the service
docker-compose stop

# Copy database
cp data/MailUptime.db data/MailUptime.db.backup.$(date +%Y%m%d)

# Restart service
docker-compose start
```

Or backup while running (SQLite supports this):

```bash
sqlite3 data/MailUptime.db ".backup data/MailUptime.db.backup"
```

## Advanced Configuration

### Using Specific Version

Edit `docker-compose.yml` to pin a specific version:

```yaml
services:
  mailuptime:
    image: mcasamento/mailuptime:v1.0.0  # Use specific version
```

### Multiple Instances

Run multiple instances on different ports:

```yaml
version: '3.8'

services:
  mailuptime-prod:
    image: mcasamento/mailuptime:latest
    container_name: mailuptime-prod
    ports:
      - "5000:8080"
    volumes:
      - ./config/prod/appsettings.json:/app/appsettings.json:ro
      - ./data/prod:/app/data
    environment:
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/MailUptime.db
    restart: unless-stopped

  mailuptime-staging:
    image: mcasamento/mailuptime:latest
    container_name: mailuptime-staging
    ports:
      - "5001:8080"
    volumes:
      - ./config/staging/appsettings.json:/app/appsettings.json:ro
      - ./data/staging:/app/data
    environment:
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/MailUptime.db
    restart: unless-stopped
```

### Health Checks

The Docker Compose file includes a health check that:
- Pings the `/api/dashboard/mailboxes` endpoint
- Checks every 30 seconds
- Waits 40 seconds before first check
- Fails after 3 consecutive failures

View health status:

```bash
docker-compose ps
```

### Resource Limits

Add resource constraints to prevent excessive resource usage:

```yaml
services:
  mailuptime:
    # ... other config ...
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
```

## Monitoring with Uptime Kuma

### Add Monitor in Uptime Kuma

1. **Monitor Type**: HTTP(s)
2. **Friendly Name**: `MailUptime - my-mailbox`
3. **URL**: `http://your-docker-host:5000/api/MailUptime/received-today/my-mailbox`
4. **Method**: GET
5. **Expected Status Code**: 200
6. **Heartbeat Interval**: 60 seconds

### Docker Network Integration

If Uptime Kuma is also running in Docker, you can use Docker networking:

```yaml
version: '3.8'

networks:
  monitoring:
    name: monitoring
    external: true  # Assumes you have a 'monitoring' network

services:
  mailuptime:
    image: mcasamento/mailuptime:latest
    container_name: mailuptime
    networks:
      - monitoring
    # ... rest of config ...
```

Then in Uptime Kuma, use the internal hostname:

```
http://mailuptime:8080/api/MailUptime/received-today/my-mailbox
```

## Troubleshooting

### Container Won't Start

Check logs:

```bash
docker-compose logs mailuptime
```

Common issues:
- **Port already in use**: Change external port in `docker-compose.yml`
- **Config file not found**: Ensure `config/appsettings.json` exists
- **Permission denied**: Check file permissions and SELinux/AppArmor settings

### Database Locked Error

This usually means multiple instances are accessing the same database:

```bash
# Check running containers
docker ps | grep mailuptime

# Stop all instances
docker-compose down

# Restart single instance
docker-compose up -d
```

### Configuration Not Applied

After changing `config/appsettings.json`:

```bash
# Restart is required
docker-compose restart

# Or recreate container
docker-compose up -d --force-recreate
```

### Connection Refused to Mail Server

Check from inside the container:

```bash
docker-compose exec mailuptime bash
curl -v telnet://imap.gmail.com:993
```

This could indicate:
- Firewall blocking outbound connections
- DNS resolution issues
- Incorrect host/port in configuration

### Database File Not Created

Ensure the `data/` directory exists and has proper permissions:

```bash
mkdir -p data
chmod 755 data
docker-compose restart
```

### Health Check Failing

View health check status:

```bash
docker inspect --format='{{json .State.Health}}' mailuptime | jq
```

Common causes:
- Application still starting (wait 40 seconds)
- Configuration error preventing startup
- Network connectivity issues

## Security Best Practices

1. **File Permissions**
   ```bash
   chmod 600 config/appsettings.json  # Only owner can read/write
   chmod 700 config                    # Only owner can access directory
   chmod 755 data                      # Owner full access, others read/execute
   ```

2. **Firewall**
   - Only expose port 5000 if needed externally
   - Use reverse proxy (nginx/traefik) for TLS termination
   - Restrict access to specific IP ranges if possible

3. **Credentials**
   - Use app-specific passwords (not main account password)
   - Rotate passwords regularly
   - Consider using Docker secrets in Swarm mode

4. **Updates**
   - Regularly update to latest version: `docker-compose pull && docker-compose up -d`
   - Subscribe to GitHub releases for security updates
   - Test updates in staging before production

## Integration with Reverse Proxy

### Nginx Example

```nginx
server {
    listen 80;
    server_name mailuptime.example.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Traefik Example

```yaml
services:
  mailuptime:
    image: mcasamento/mailuptime:latest
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.mailuptime.rule=Host(`mailuptime.example.com`)"
      - "traefik.http.routers.mailuptime.entrypoints=websecure"
      - "traefik.http.routers.mailuptime.tls.certresolver=letsencrypt"
      - "traefik.http.services.mailuptime.loadbalancer.server.port=8080"
    # ... rest of config ...
```

## Support

For issues and questions:
- **Documentation**: See [README.md](README.md) and [CONFIGURATION.md](CONFIGURATION.md)
- **GitHub Issues**: https://github.com/marcoCasamento/MailUptime/issues
- **Docker Hub**: https://hub.docker.com/r/mcasamento/mailuptime

---

**Happy Monitoring with Docker! ????**
