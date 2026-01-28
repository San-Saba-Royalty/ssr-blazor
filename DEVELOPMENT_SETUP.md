# Development Setup Guide

## Quick Start

Choose your preferred development environment:
- **[macOS Native Setup](#macos-development-requirements)** - Run directly on macOS with Homebrew
- **[Docker Setup](./docs/DOCKER_DEPLOYMENT.md)** - Run in Docker container (cross-platform)

---

## macOS Development Requirements

### Install libgdiplus

FastReport requires libgdiplus for graphics operations on macOS. Install it using Homebrew:

```bash
brew install mono-libgdiplus
```

### Create Symlink (Required)

.NET may not automatically find the library in Homebrew's location. Create a symlink to a standard location:

```bash
sudo mkdir -p /usr/local/lib
sudo ln -sf /opt/homebrew/lib/libgdiplus.dylib /usr/local/lib/libgdiplus.dylib
```

### Verify Installation

Check that the library is properly installed and linked:

```bash
# Check Homebrew installation
ls -la /opt/homebrew/lib/libgdiplus.*

# Check symlink
ls -la /usr/local/lib/libgdiplus.dylib
```

### Rider IDE Configuration (Optional)

If the application still can't find libgdiplus when running from Rider:

1. Go to **Run** → **Edit Configurations**
2. Select your SSRBlazor run configuration
3. Add Environment Variable: `DYLD_LIBRARY_PATH=/opt/homebrew/lib`
4. Apply and restart

## Troubleshooting

### Error: "Unable to load shared library 'libgdiplus'"

**Solution 1**: Verify installation
```bash
brew list mono-libgdiplus
```

**Solution 2**: Check library location
```bash
ls -la /opt/homebrew/lib/libgdiplus.*
```

**Solution 3**: Recreate symlink
```bash
sudo ln -sf /opt/homebrew/lib/libgdiplus.dylib /usr/local/lib/libgdiplus.dylib
```

**Solution 4**: Set environment variable
```bash
export DYLD_LIBRARY_PATH=/opt/homebrew/lib:$DYLD_LIBRARY_PATH
```

Add to `~/.zshrc` for persistence:
```bash
echo 'export DYLD_LIBRARY_PATH=/opt/homebrew/lib:$DYLD_LIBRARY_PATH' >> ~/.zshrc
source ~/.zshrc
```

### Homebrew Not Installed

Install Homebrew first:
```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

### Permission Denied

Use `sudo` for the symlink command or adjust security settings.

## Production Deployment

### Linux Servers (Ubuntu/Debian)

```bash
sudo apt-get update
sudo apt-get install -y libgdiplus
```

### Linux Servers (CentOS/RHEL)

```bash
sudo yum install -y libgdiplus
```

### Docker Containers

Add to your Dockerfile:

```dockerfile
# For Debian-based images
RUN apt-get update && \
    apt-get install -y libgdiplus && \
    rm -rf /var/lib/apt/lists/*

# For Alpine-based images
RUN apk add --no-cache libgdiplus
```

### Windows Servers

No action needed - GDI+ is built into Windows.

## Testing the Fix

After installation, run the application and test report generation:

```bash
cd /Users/gqadonis/RiderProjects/SSRBlazor
dotnet run
```

Navigate to a report feature and verify it generates without errors.

## Success Indicators

✅ No `DllNotFoundException` for libgdiplus  
✅ FastReport initializes successfully  
✅ Reports generate without errors  
✅ Application runs in both terminal and IDE

## Docker Alternative

For a consistent environment across all platforms, consider using Docker:

```bash
cd /Users/gqadonis/RiderProjects
docker-compose -f SSRBlazor/docker-compose.yaml up --build
```

See [Docker Deployment Guide](./docs/DOCKER_DEPLOYMENT.md) for complete instructions.

## Additional Resources

- [Docker Deployment Guide](./docs/DOCKER_DEPLOYMENT.md) - Complete Docker setup with libgdiplus
- [FastReport Documentation](https://docs.fast-report.com/)
- [Homebrew Documentation](https://docs.brew.sh/)
- [Detailed troubleshooting guide](./plans/libgdiplus-fix-plan.md)
