# Docker Compose Troubleshooting Guide

This guide covers common issues when running MailUptime with Docker Compose.

---

## Issue #1: appsettings.json Created as Directory

### Symptoms
```bash
$ ls -la config/
drwxr-xr-x  2 user user 4096 Feb  9 18:06 appsettings.json  # ? It's a directory!
```

Or container logs show:
```
Cannot find configuration file /app/appsettings.json
```

### Why This Happens

**Docker volume mount behavior:** When you run `docker compose up` and the source file `./config/appsettings.json` **does not exist**, Docker:
1. Doesn't know whether it should be a file or directory
2. Assumes it's a directory and creates it
3. Mounts an empty directory instead of failing

This is a common Docker gotcha!

### Solution

```bash
# 1. Stop container
docker compose down

# 2. Remove the directory
rm -rf config/appsettings.json

# 3. Create the file properly
cat > config/appsettings.json << 'EOF'
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
    "ExpectedSenderEmails": [],
    "ReportConfig": [
      {
        "Name": "my-mailbox",
        "ExpectedSubjectPattern": ".*",
        "ExpectedBodyPattern": ".*"
      }
    ]
  }
}
EOF

# 4. Edit with your credentials
nano config/appsettings.json

# 5. Verify it's a file
file config/appsettings.json
# Output: config/appsettings.json: JSON data

# 6. Fix permissions
chmod 600 config/appsettings.json

# 7. Start container
docker compose up -d
```

### Prevention

**Always create `config/appsettings.json` BEFORE running `docker compose up`!**

Use the setup script:
```bash
./docker-setup.sh  # Creates file structure properly
```

Or create manually:
```bash
mkdir -p config data
# Create the file first (not the directory!)
touch config/appsettings.json
# Then edit it
nano config/appsettings.json
```

---

## Issue #2: Volume Mount Permission Error (WSL/Docker Desktop)

### Symptoms
```
Error response from daemon: failed to create task for container: 
failed to create shim task: OCI runtime create failed: 
error mounting "/run/desktop/mnt/host/wsl/..." to rootfs at "/app/appsettings.json": 
no such file or directory: unknown
```

### Why This Happens

On WSL/Docker Desktop:
- Files are owned by `root:root`
- Docker daemon runs as your user
- Volume mount fails due to permission mismatch

### Solution

```bash
# Fix ownership
sudo chown -R $USER:$USER config data

# Verify
ls -la config/ data/

# Restart container
docker compose down
docker compose up -d
```

### Prevention

Run setup script as your regular user (not with sudo):
```bash
./docker-setup.sh  # ? Correct
# sudo ./docker-setup.sh  # ? Wrong - creates root-owned files
```

The setup script now automatically fixes ownership issues.

---

## Issue #3: Port Already in Use

### Symptoms
```
Error starting userland proxy: listen tcp4 0.0.0.0:5000: bind: address already in use
```

### Solution

**Option A: Change the external port**

Edit `docker-compose.yml`:
```yaml
ports:
  - "8080:8080"  # Changed from 5000:8080
```

**Option B: Stop the conflicting service**
```bash
# Find what's using port 5000
sudo lsof -i :5000  # Linux/macOS
netstat -ano | findstr :5000  # Windows

# Stop the conflicting service or use a different port
```

---

## Issue #4: Container Not Starting

### Symptoms
```bash
$ docker compose ps
NAME         IMAGE     COMMAND   SERVICE   CREATED   STATUS
```

### Diagnosis

Check logs:
```bash
docker compose logs mailuptime
```

Common causes:
- Configuration file syntax error
- Missing required configuration fields
- Permission issues
- Port conflicts

### Solution

**Validate configuration:**
```bash
# Check JSON syntax
cat config/appsettings.json | jq .
# If jq is not installed, use online validator: jsonlint.com
```

**Check file permissions:**
```bash
ls -la config/appsettings.json
# Should be: -rw------- (600)
```

**Force recreate:**
```bash
docker compose down
docker compose up -d --force-recreate
```

---

## Issue #5: Database Locked Error

### Symptoms
```
Microsoft.Data.Sqlite.SqliteException: database is locked
```

### Why This Happens

Multiple containers trying to access the same SQLite database.

### Solution

```bash
# Check for multiple instances
docker ps -a | grep mailuptime

# Stop all instances
docker compose down

# Remove any stopped containers
docker rm mailuptime

# Start single instance
docker compose up -d

# Verify only one is running
docker ps | grep mailuptime
```

---

## Issue #6: Configuration Changes Not Applied

### Symptoms

Changed `config/appsettings.json` but container still uses old settings.

### Why This Happens

Container needs to be restarted to reload configuration.

### Solution

```bash
# Restart container
docker compose restart

# Or force recreate
docker compose up -d --force-recreate

# Verify new config is loaded (check logs)
docker compose logs -f mailuptime
```

---

## Issue #7: Cannot Connect to Mail Server

### Symptoms
```
Connection refused to imap.gmail.com:993
```

### Diagnosis

Test from inside container:
```bash
docker compose exec mailuptime bash

# Test DNS resolution
nslookup imap.gmail.com

# Test connectivity
curl -v telnet://imap.gmail.com:993

# Exit container
exit
```

### Common Causes

1. **Firewall blocking outbound connections**
   - Check corporate firewall rules
   - Verify Docker networking is allowed

2. **DNS resolution issues**
   - Use IP address instead of hostname temporarily
   - Check `/etc/resolv.conf` in container

3. **Wrong host/port in configuration**
   - Gmail IMAP: `imap.gmail.com:993` (SSL)
   - Gmail POP3: `pop.gmail.com:995` (SSL)

4. **App-specific password not enabled** (Gmail)
   - Enable 2FA in Google Account
   - Generate app-specific password
   - Use app password instead of account password

---

## Issue #8: Health Check Failing

### Symptoms
```bash
$ docker compose ps
NAME        IMAGE     STATUS (unhealthy)
```

### Diagnosis
```bash
# Check health check logs
docker inspect mailuptime | jq '.[0].State.Health'

# Manual health check
docker compose exec mailuptime curl -f http://localhost:8080/api/dashboard/mailboxes
```

### Common Causes

1. **Application not started yet**
   - Wait for `start_period: 40s` to pass
   - Check logs: `docker compose logs -f mailuptime`

2. **API endpoint not responding**
   - Check if app crashed
   - Review application logs for errors

3. **curl not installed in container**
   - Should be available in official image
   - Report issue if missing

---

## Issue #9: Dashboard Shows Empty/No Data

### Symptoms

Web dashboard loads but shows "No mailboxes configured"

### Diagnosis

1. **Check configuration loaded:**
```bash
docker compose exec mailuptime cat /app/appsettings.json
```

2. **Check logs for errors:**
```bash
docker compose logs mailuptime | grep -i error
```

3. **Verify database created:**
```bash
ls -la data/
# Should see: MailUptime.db
```

### Solution

Ensure `ReportConfig` array has at least one entry:
```json
{
  "MailboxSettings": {
    "ReportConfig": [
      {
        "Name": "my-mailbox",
        "ExpectedSubjectPattern": ".*",
        "ExpectedBodyPattern": ".*"
      }
    ]
  }
}
```

---

## Issue #10: Data Not Persisting

### Symptoms

Database resets every time container restarts.

### Diagnosis

```bash
# Check volume mount
docker compose exec mailuptime ls -la /app/data/

# Check external data directory
ls -la data/
```

### Solution

Ensure volume is properly mounted in `docker-compose.yml`:
```yaml
volumes:
  - ./data:/app/data
```

Verify directory exists:
```bash
mkdir -p data
chmod 755 data
docker compose down && docker compose up -d
```

---

## Quick Diagnostic Commands

```bash
# Container status
docker compose ps

# View logs (last 50 lines)
docker compose logs --tail=50 mailuptime

# Follow logs in real-time
docker compose logs -f mailuptime

# Inspect container
docker inspect mailuptime

# Check mounts
docker inspect mailuptime | jq '.[0].Mounts'

# Shell into container
docker compose exec mailuptime bash

# Check file ownership
ls -la config/ data/

# Verify config file
file config/appsettings.json
cat config/appsettings.json | jq .

# Check ports
docker compose port mailuptime 8080

# View resource usage
docker stats mailuptime

# Full cleanup and restart
docker compose down -v
docker compose up -d --force-recreate
```

---

## Getting Help

If none of these solutions work:

1. **Gather diagnostics:**
```bash
docker compose logs --tail=100 mailuptime > logs.txt
docker compose ps >> logs.txt
docker compose config >> logs.txt
cat config/appsettings.json | jq . >> logs.txt  # Remove sensitive data first!
```

2. **Check documentation:**
   - [DOCKER_COMPOSE.md](DOCKER_COMPOSE.md)
   - [CONFIGURATION.md](CONFIGURATION.md)
   - [README.md](README.md)

3. **Open an issue:**
   - Repository: https://github.com/marcoCasamento/MailUptime/issues
   - Include logs and configuration (redact passwords!)

---

## Preventive Checklist

Before running `docker compose up`:

- [ ] `config/appsettings.json` exists as a **file** (not directory)
- [ ] Configuration has valid JSON syntax
- [ ] Files owned by your user (not root)
- [ ] Directories `config/` and `data/` exist
- [ ] Port 5000 is available
- [ ] Docker and Docker Compose are installed and running
- [ ] Proper credentials configured (app-specific passwords for Gmail)

Run the setup script to ensure everything is correct:
```bash
./docker-setup.sh
```
