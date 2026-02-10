# Docker Image Tagging Strategy

## Overview

The MailUptime Docker image uses automated tagging via GitHub Actions to ensure proper versioning and the `latest` tag assignment.

## How It Works

### Automatic Tagging

When you create a **release** or push a **git tag** matching `v*.*.*` (e.g., `v1.2.3`), the GitHub Actions workflow automatically builds and pushes the Docker image with multiple tags:

**Example: Creating release `v1.2.3`**

The image will be tagged with:
- `mcasamento/mailuptime:1.2.3` (full semver)
- `mcasamento/mailuptime:1.2` (major.minor)
- `mcasamento/mailuptime:1` (major only)
- `mcasamento/mailuptime:latest` ?
- `mcasamento/mailuptime:sha-abc1234` (git commit SHA)

## Creating a Release (Recommended)

### Option 1: GitHub Web UI

1. Go to your repository: https://github.com/marcoCasamento/MailUptime
2. Click **Releases** ? **Create a new release**
3. Click **Choose a tag**
4. Type your version (e.g., `v1.2.3`)
5. Click **Create new tag: v1.2.3 on publish**
6. Fill in release title and description
7. Click **Publish release**

GitHub Actions will automatically:
- Build the Docker image
- Tag it with version and `latest`
- Push to Docker Hub
- Update the Docker Hub description

### Option 2: Git Command Line

```bash
# Ensure you're on master and up to date
git checkout master
git pull origin master

# Create and push a tag
git tag v1.2.3
git push origin v1.2.3

# Or create a tag with message
git tag -a v1.2.3 -m "Release version 1.2.3 - Added dashboard improvements"
git push origin v1.2.3
```

Then create the release on GitHub:
```bash
# Using GitHub CLI (if installed)
gh release create v1.2.3 --title "v1.2.3" --notes "Release notes here"
```

## Tagging Rules

### When `latest` Tag is Applied

The `latest` tag is automatically applied when:
- ? A GitHub **release** is published
- ? Code is pushed to the **master** branch
- ? A tag matching `v*.*.*` is pushed

### When `latest` Tag is NOT Applied

- ? Feature branch pushes
- ? Pull request builds
- ? Manual workflow runs without proper tag

## Versioning Best Practices

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR** version (v2.0.0): Incompatible API changes
- **MINOR** version (v1.1.0): New features, backwards compatible
- **PATCH** version (v1.0.1): Bug fixes, backwards compatible

### Examples

```bash
# Patch release (bug fixes)
git tag v1.0.1

# Minor release (new features)
git tag v1.1.0

# Major release (breaking changes)
git tag v2.0.0
```

## Checking Your Published Images

### Docker Hub Web UI
Visit: https://hub.docker.com/r/mcasamento/mailuptime/tags

### Command Line
```bash
# List all tags
curl -s https://registry.hub.docker.com/v2/repositories/mcasamento/mailuptime/tags | jq -r '.results[].name'

# Check latest digest
docker manifest inspect mcasamento/mailuptime:latest
```

## Workflow Triggers

The Docker build workflow runs on:

1. **Tag push** matching `v*.*.*`
   ```bash
   git tag v1.2.3 && git push origin v1.2.3
   ```

2. **Release published** via GitHub UI

3. **Manual dispatch** via GitHub Actions UI
   - Go to Actions ? Docker Build and Push ? Run workflow
   - Specify custom tag if needed

## Multi-Architecture Support

Images are built for:
- `linux/amd64` (Intel/AMD x64)
- `linux/arm64` (ARM 64-bit, e.g., Raspberry Pi 4, Apple Silicon)

Docker automatically pulls the correct architecture for your system.

## Local Testing Before Release

Test the Docker build locally before creating a release:

```bash
# Build locally
docker build -t mailuptime:test .

# Test the image
docker run -p 5000:8080 \
  -v ./config/appsettings.json:/app/appsettings.json:ro \
  -v ./data:/app/data \
  mailuptime:test

# Verify it works
curl http://localhost:5000/api/dashboard/mailboxes
```

## Troubleshooting

### Release didn't trigger build

**Check:**
1. Tag format is `v*.*.*` (e.g., `v1.2.3` not `1.2.3`)
2. Workflow file is in `.github/workflows/docker-publish.yml`
3. GitHub Actions is enabled in repository settings
4. Docker Hub credentials are set in repository secrets:
   - `DOCKER_USERNAME`
   - `DOCKER_PASSWORD`

### `latest` tag not updating

**Check:**
1. Release was published (not just a draft)
2. Tag was pushed to master branch
3. Check workflow run logs in GitHub Actions

### Manual workflow run

If automated triggers fail, manually run the workflow:

1. Go to **Actions** tab
2. Select **Docker Build and Push**
3. Click **Run workflow**
4. Select branch and tag
5. Click **Run workflow**

## Required Secrets

Ensure these secrets are configured in your repository:

**Settings ? Secrets and variables ? Actions ? Repository secrets**

- `DOCKER_USERNAME`: Your Docker Hub username
- `DOCKER_PASSWORD`: Docker Hub access token (recommended) or password

### Creating Docker Hub Access Token

1. Log in to https://hub.docker.com
2. Account Settings ? Security ? Access Tokens
3. Click **New Access Token**
4. Name: `GitHub Actions MailUptime`
5. Permissions: **Read & Write**
6. Copy the token and add to GitHub secrets

## Quick Reference

```bash
# Create and publish a release
git tag -a v1.2.3 -m "Release v1.2.3"
git push origin v1.2.3
gh release create v1.2.3 --generate-notes

# Pull the latest image
docker pull mcasamento/mailuptime:latest

# Pull specific version
docker pull mcasamento/mailuptime:1.2.3

# Check what version is running
docker inspect mcasamento/mailuptime:latest | jq '.[0].Config.Labels'
```

## See Also

- [GitHub Actions Documentation](GITHUB_ACTIONS.md)
- [Docker Compose Guide](DOCKER_COMPOSE.md)
- [Contributing Guidelines](CONTRIBUTING.md)
