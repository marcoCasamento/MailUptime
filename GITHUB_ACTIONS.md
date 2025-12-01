# GitHub Actions & Docker Hub Setup

This project uses GitHub Actions for continuous integration and Docker image publishing.

## Workflows

### 1. Build and Test (`build.yml`)

**Trigger:** Push to main/master branches, pull requests, manual dispatch

**Purpose:** Validates code changes by building and testing the project

**Steps:**
1. Checks out the code
2. Sets up .NET 10
3. Restores NuGet packages
4. Builds the project in Release configuration
5. Runs tests (if any)
6. Publishes the application
7. Uploads build artifacts (retained for 7 days)

**Status Badge:**
```markdown
[![Build Status](https://github.com/marcoCasamento/MailUptime/actions/workflows/build.yml/badge.svg)](https://github.com/marcoCasamento/MailUptime/actions/workflows/build.yml)
```

### 2. Docker Build and Push (`docker-publish.yml`)

**Trigger:** 
- Version tags (e.g., `v1.0.0`)
- Published releases
- Manual dispatch

**Purpose:** Builds and publishes multi-architecture Docker images to Docker Hub

**Features:**
- Multi-platform builds (linux/amd64, linux/arm64)
- Automatic semantic versioning from git tags
- Layer caching for faster builds
- Updates Docker Hub repository description

**Tags Generated:**
- `latest` (for default branch)
- `v1.2.3` (full semantic version)
- `v1.2` (major.minor)
- `v1` (major version)
- `main-<sha>` (branch + commit SHA)

**Status Badge:**
```markdown
[![Docker Build](https://github.com/marcoCasamento/MailUptime/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/marcoCasamento/MailUptime/actions/workflows/docker-publish.yml)
```

## Required GitHub Secrets

To enable Docker Hub publishing, you need to add these secrets to your GitHub repository:

### Setting Up Secrets

1. Go to your GitHub repository
2. Navigate to **Settings** ? **Secrets and variables** ? **Actions**
3. Click **New repository secret**
4. Add the following secrets:

| Secret Name | Description | Example |
|------------|-------------|---------|
| `DOCKER_USERNAME` | Your Docker Hub username | `johndoe` |
| `DOCKER_PASSWORD` | Docker Hub access token or password | `dckr_pat_xxxxx` (recommended: use access token) |

### Creating a Docker Hub Access Token (Recommended)

Instead of using your Docker Hub password, create an access token:

1. Log in to [Docker Hub](https://hub.docker.com/)
2. Go to **Account Settings** ? **Security**
3. Click **New Access Token**
4. Name it (e.g., "GitHub Actions")
5. Select **Read, Write, Delete** permissions
6. Copy the token and use it as `DOCKER_PASSWORD` secret

## Pulling Docker Images

Once the workflows are set up and run successfully, you can pull images from Docker Hub:

```bash
# Pull latest version
docker pull mcasamento/mailuptime:latest

# Pull specific version
docker pull mcasamento/mailuptime:v1.0.0

# Pull specific major version (gets latest 1.x.x)
docker pull mcasamento/mailuptime:v1
```

## Creating a Release

To trigger a Docker image build and publish:

### Option 1: Create a Git Tag

```bash
git tag v1.0.0
git push origin v1.0.0
```

### Option 2: Create a GitHub Release

1. Go to your repository on GitHub
2. Click **Releases** ? **Create a new release**
3. Choose or create a tag (e.g., `v1.0.0`)
4. Fill in release title and description
5. Click **Publish release**

### Option 3: Manual Dispatch

1. Go to **Actions** tab on GitHub
2. Select **Docker Build and Push** workflow
3. Click **Run workflow**
4. Enter desired tag name
5. Click **Run workflow**

## Workflow Status

Monitor your workflows:
- Visit the **Actions** tab in your GitHub repository
- Click on individual workflow runs to see detailed logs
- Failed builds will show error messages and can be re-run

## Multi-Architecture Support

The Docker workflow builds images for multiple architectures:
- **linux/amd64**: Standard x86-64 systems
- **linux/arm64**: ARM-based systems (Raspberry Pi 4+, AWS Graviton, Apple Silicon)

Docker will automatically pull the correct architecture for your system.

## Best Practices

1. **Version Tagging**: Use semantic versioning (v1.0.0, v1.1.0, v2.0.0)
2. **Access Tokens**: Use Docker Hub access tokens instead of passwords
3. **Testing**: Always test your changes locally before pushing
4. **Release Notes**: Include detailed release notes when creating releases
5. **Breaking Changes**: Bump major version (v2.0.0) for breaking changes

## Troubleshooting

### Build Fails on GitHub but Works Locally

- Ensure all dependencies are properly restored
- Check for environment-specific code
- Review GitHub Actions logs for specific errors

### Docker Push Fails

- Verify `DOCKER_USERNAME` and `DOCKER_PASSWORD` secrets are set correctly
- Ensure Docker Hub access token has write permissions
- Check if repository name matches format: `username/mailuptime`

### Multi-Arch Build Takes Too Long

- This is normal for the first build
- Subsequent builds use layer caching and are much faster
- Consider removing arm64 if not needed for your use case

## Customization

### Change Docker Hub Repository Name

Edit `.github/workflows/docker-publish.yml`:

```yaml
images: ${{ secrets.DOCKER_USERNAME }}/your-custom-name
```

### Add Additional Platforms

Edit the `platforms` line in `docker-publish.yml`:

```yaml
platforms: linux/amd64,linux/arm64,linux/arm/v7
```

### Disable Automatic Description Updates

Remove or comment out the "Update Docker Hub description" step in `docker-publish.yml`.

## Links

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Docker Build Push Action](https://github.com/docker/build-push-action)
- [Docker Hub](https://hub.docker.com/)
