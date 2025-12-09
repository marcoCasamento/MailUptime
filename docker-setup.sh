#!/bin/bash

# MailUptime Docker Compose Setup Script
# This script helps you set up and start MailUptime with Docker Compose

set -e

echo "=========================================="
echo "MailUptime Docker Compose Setup"
echo "=========================================="
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "? Error: Docker is not installed"
    echo "Please install Docker first: https://docs.docker.com/get-docker/"
    exit 1
fi

# Check if Docker Compose is available
if ! docker compose version &> /dev/null; then
    echo "? Error: Docker Compose is not available"
    echo "Please install Docker Compose: https://docs.docker.com/compose/install/"
    exit 1
fi

echo "? Docker and Docker Compose are installed"
echo ""

# Create directories if they don't exist
echo "Creating directory structure..."
mkdir -p config data

# Check if config file exists
if [ -f "config/appsettings.json" ]; then
    echo "? Configuration file found: config/appsettings.json"
else
    echo "??  Configuration file not found"
    echo "Creating example configuration..."
    cat > config/appsettings.json << 'EOF'
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
    echo "? Created config/appsettings.json"
    echo ""
    echo "??  IMPORTANT: Edit config/appsettings.json with your email credentials before starting!"
    echo ""
    read -p "Press Enter to open the config file in nano editor (or Ctrl+C to exit)..."
    nano config/appsettings.json || vi config/appsettings.json || echo "Please edit config/appsettings.json manually"
fi

echo ""
echo "Setting file permissions..."
chmod 600 config/appsettings.json
chmod 755 data

echo "? Permissions set"
echo ""

# Check if container is already running
if docker ps | grep -q mailuptime; then
    echo "??  MailUptime container is already running"
    read -p "Do you want to restart it? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "Restarting container..."
        docker compose restart
    fi
else
    # Pull latest image
    echo "Pulling latest Docker image..."
    docker compose pull
    
    echo ""
    echo "Starting MailUptime..."
    docker compose up -d
    
    echo ""
    echo "? MailUptime is starting..."
    echo ""
    echo "Waiting for service to be ready..."
    sleep 5
    
    # Check if container is running
    if docker ps | grep -q mailuptime; then
        echo "? Container is running!"
    else
        echo "? Container failed to start. Checking logs..."
        docker compose logs mailuptime
        exit 1
    fi
fi

echo ""
echo "=========================================="
echo "Setup Complete!"
echo "=========================================="
echo ""
echo "Access points:"
echo "  • Web Dashboard: http://localhost:5000/"
echo "  • API: http://localhost:5000/api/dashboard/mailboxes"
echo "  • Health Check: http://localhost:5000/api/MailUptime/received-today/my-mailbox"
echo ""
echo "Useful commands:"
echo "  • View logs: docker compose logs -f"
echo "  • Stop service: docker compose stop"
echo "  • Restart service: docker compose restart"
echo "  • Update image: docker compose pull && docker compose up -d"
echo ""
echo "Documentation:"
echo "  • Full guide: DOCKER_COMPOSE.md"
echo "  • Quick reference: DOCKER_QUICK_REF.md"
echo "  • Configuration: CONFIGURATION.md"
echo ""

# Try to open browser (optional)
if command -v xdg-open &> /dev/null; then
    read -p "Open web dashboard in browser? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        xdg-open http://localhost:5000/
    fi
elif command -v open &> /dev/null; then
    read -p "Open web dashboard in browser? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        open http://localhost:5000/
    fi
fi

echo "Happy monitoring! ???"
