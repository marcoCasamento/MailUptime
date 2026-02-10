# Quick Release Guide

## TL;DR - Create a Release

```bash
# 1. Ensure you're on master and up to date
git checkout master
git pull origin master

# 2. Create and push a version tag
git tag v1.2.3
git push origin v1.2.3

# 3. Create GitHub release (creates Docker image with 'latest' tag)
gh release create v1.2.3 --generate-notes
```

That's it! GitHub Actions will automatically:
- Build Docker image
- Tag it with `v1.2.3`, `v1.2`, `v1`, `latest`
- Push to Docker Hub
- Update repository description

## Without GitHub CLI

### Create Tag
```bash
git tag -a v1.2.3 -m "Release v1.2.3 - Bug fixes and improvements"
git push origin v1.2.3
```

### Create Release on GitHub
1. Go to https://github.com/marcoCasamento/MailUptime/releases
2. Click **"Draft a new release"**
3. Click **"Choose a tag"** ? Select `v1.2.3`
4. Click **"Generate release notes"** (or write your own)
5. Click **"Publish release"**

## Version Numbering

Use [Semantic Versioning](https://semver.org/): `vMAJOR.MINOR.PATCH`

- **PATCH** (v1.0.1): Bug fixes, no new features
- **MINOR** (v1.1.0): New features, backwards compatible
- **MAJOR** (v2.0.0): Breaking changes

## Verify Release

```bash
# Check Docker Hub
docker pull mcasamento/mailuptime:latest
docker pull mcasamento/mailuptime:v1.2.3

# Verify tags match
docker images mcasamento/mailuptime

# Check version
docker inspect mcasamento/mailuptime:latest | jq '.[0].Config.Labels'
```

## For Full Details

See [DOCKER_TAGGING.md](DOCKER_TAGGING.md)
