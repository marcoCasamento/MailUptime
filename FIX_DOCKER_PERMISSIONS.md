# Fix Docker Volume Mount Issues

## Common Problem #1: Permission Error

You're seeing this error:
```
error mounting "..." to rootfs at "/app/appsettings.json": no such file or directory
```

This happens on **WSL/Docker Desktop** when configuration files are owned by `root` but Docker needs to mount them as your user.

**Solution:**
```bash
# Stop the container
docker compose down

# Fix file ownership
sudo chown -R $USER:$USER config data

# Verify ownership changed
ls -la config/ data/

# Start the container
docker compose up -d
```

---

## Common Problem #2: appsettings.json Created as Directory

You see a **folder** named `appsettings.json` instead of a file:

```bash
ls -la config/
drwxr-xr-x  2 user user 4096 Feb  9 18:06 appsettings.json  # ? Directory!
```

**Why This Happens:**

When you run `docker compose up` **before** creating the `config/appsettings.json` file, Docker doesn't know it should be a file. So it creates it as a **directory** instead!

**Solution:**

```bash
# Stop container
docker compose down

# Remove the directory
rm -rf config/appsettings.json

# Create the file properly
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

# Verify it's a file now
file config/appsettings.json
# Should output: config/appsettings.json: JSON data

# Start container
docker compose up -d
```

**Prevention:**
Always create `config/appsettings.json` **BEFORE** running `docker compose up` for the first time!

---

## The Problem

You're seeing this error:
```
error mounting "..." to rootfs at "/app/appsettings.json": no such file or directory
```

This happens on **WSL/Docker Desktop** when configuration files are owned by `root` but Docker needs to mount them as your user.

## Quick Fix

Run these commands in your `~/mailuptime` directory:

```bash
# Stop the container
docker compose down

# Fix file ownership
sudo chown -R $USER:$USER config data

# Verify ownership changed
ls -la config/ data/

# Start the container
docker compose up -d

# Check if it's working
docker compose logs -f mailuptime
```

## Verify It's Working

```bash
# Container should be running
docker ps | grep mailuptime

# Should return 200 OK
curl -I http://localhost:5000/api/dashboard/mailboxes

# Open dashboard
# http://localhost:5000/
```

## Why This Happened

When you created the config files with `sudo` or as root user, Docker Desktop on WSL couldn't mount them because:
1. Files owned by `root:root`
2. Docker daemon runs as your user (`marco`)
3. Volume mount permissions fail

## Prevention

**Always run setup scripts as your regular user:**

? **Correct:**
```bash
./docker-setup.sh
```

? **Wrong:**
```bash
sudo ./docker-setup.sh  # Creates root-owned files
```

## Alternative: Use Docker Volumes

If you prefer not to use bind mounts, modify `docker-compose.yml`:

```yaml
services:
  mailuptime:
    volumes:
      # Use named volumes instead of bind mounts
      - mailuptime-config:/app/config
      - mailuptime-data:/app/data

volumes:
  mailuptime-config:
  mailuptime-data:
```

Then copy config into the volume:
```bash
docker compose up -d
docker cp config/appsettings.json mailuptime:/app/appsettings.json
docker compose restart
```

## Need More Help?

- Full guide: `DOCKER_COMPOSE.md`
- Quick reference: `DOCKER_QUICK_REF.md`
- View logs: `docker compose logs -f mailuptime`
