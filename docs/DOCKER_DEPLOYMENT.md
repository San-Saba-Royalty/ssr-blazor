# Docker Deployment Guide for SSRBlazor

## Overview

The Dockerfile and docker-compose configuration have been updated to include all necessary dependencies for running SSRBlazor in a containerized environment, including libgdiplus for FastReport support.

## Dockerfile Features

The updated [`Dockerfile`](../../../RiderProjects/SSRBlazor/Dockerfile) includes:

- ✅ **libgdiplus installation** - Required for FastReport and System.Drawing
- ✅ **Multi-stage build** - Optimized image size
- ✅ **SSRBusiness.NET10 project reference** - Proper project dependency handling
- ✅ **.NET 10.0 runtime** - Latest .NET version support

## Building the Docker Image

### Option 1: Using Docker Compose (Recommended)

From the **RiderProjects** directory:

```bash
cd /Users/gqadonis/RiderProjects
docker-compose -f SSRBlazor/docker-compose.yaml build
```

### Option 2: Using Docker Build Directly

From the **RiderProjects** directory:

```bash
cd /Users/gqadonis/RiderProjects
docker build -f SSRBlazor/Dockerfile -t ssrblazor:latest .
```

**Important**: The build context must be the parent directory (RiderProjects) because the Dockerfile references both `SSRBlazor/` and `SSRBusiness.NET10/` projects.

## Running the Container

### Using Docker Compose

```bash
cd /Users/gqadonis/RiderProjects
docker-compose -f SSRBlazor/docker-compose.yaml up
```

Access the application at: `http://localhost:8080`

### Using Docker Run

```bash
docker run -d \
  -p 8080:8080 \
  -p 8081:8081 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  --name ssrblazor \
  ssrblazor:latest
```

## Docker Configuration Details

### Dockerfile Structure

```dockerfile
# Base image with libgdiplus
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && \
    apt-get install -y libgdiplus && \
    apt-get clean

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Copies both SSRBlazor and SSRBusiness.NET10 projects
# Restores, builds, and publishes

# Final stage
FROM base AS final
# Runs the published application
```

### Key Changes from Original

1. **Added libgdiplus installation** in base stage:
   ```dockerfile
   RUN apt-get update && \
       apt-get install -y libgdiplus && \
       apt-get clean
   ```

2. **Updated COPY paths** to handle both projects:
   ```dockerfile
   COPY ["SSRBlazor/SSRBlazor.csproj", "SSRBlazor/"]
   COPY ["SSRBusiness.NET10/SSRBusiness.csproj", "SSRBusiness.NET10/"]
   ```

3. **Updated build context** in docker-compose:
   ```yaml
   context: ..  # Parent directory
   dockerfile: SSRBlazor/Dockerfile
   ```

## Environment Variables

The docker-compose configuration includes:

- `ASPNETCORE_ENVIRONMENT=Development` - Set to Production for production deployments
- `ASPNETCORE_URLS=http://+:8080` - Configure listening URLs

Add additional environment variables as needed:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ConnectionStrings__DefaultConnection=your-connection-string
  - ASPNETCORE_HTTPS_PORT=8081
```

## Volume Mounts

Configuration files can be mounted for easy updates:

```yaml
volumes:
  - ./appsettings.json:/app/appsettings.json:ro
  - ./appsettings.Development.json:/app/appsettings.Development.json:ro
```

## Troubleshooting

### Build Fails - Context Issues

**Error**: `COPY failed: file not found`

**Solution**: Ensure you're running docker build from the correct directory:
```bash
cd /Users/gqadonis/RiderProjects
docker build -f SSRBlazor/Dockerfile -t ssrblazor .
```

### Report Generation Fails

**Error**: `DllNotFoundException: libgdiplus`

**Solution**: Verify libgdiplus is installed in the image:
```bash
docker run --rm ssrblazor:latest dpkg -l | grep libgdiplus
```

Should show: `libgdiplus 6.0.x-x`

### Container Won't Start

**Check logs**:
```bash
docker logs ssrblazor
```

**Check container health**:
```bash
docker ps -a | grep ssrblazor
```

## Production Deployment

### Building for Production

```bash
docker build \
  -f SSRBlazor/Dockerfile \
  --build-arg BUILD_CONFIGURATION=Release \
  -t ssrblazor:production \
  .
```

### Production docker-compose

Create `docker-compose.production.yaml`:

```yaml
services:
  ssrblazor:
    image: ssrblazor:production
    ports:
      - "80:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

### Database Connection

Add database connection string:

```yaml
environment:
  - ConnectionStrings__DefaultConnection=Server=db;Database=SSRBlazor;User=sa;Password=YourPassword;
```

### Using Docker Secrets (Production)

For sensitive data:

```yaml
secrets:
  db_password:
    external: true

services:
  ssrblazor:
    secrets:
      - db_password
    environment:
      - DB_PASSWORD_FILE=/run/secrets/db_password
```

## Development Workflow

### Live Reload with Volumes

For development with hot reload:

```yaml
volumes:
  - ./SSRBlazor:/src/SSRBlazor
  - ./SSRBusiness.NET10:/src/SSRBusiness.NET10
```

### Debugging

Run with debug configuration:

```bash
docker run -it --rm \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  ssrblazor:latest
```

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Build Docker Image
  run: |
    docker build -f SSRBlazor/Dockerfile -t ssrblazor:${{ github.sha }} .
    
- name: Push to Registry
  run: |
    docker tag ssrblazor:${{ github.sha }} registry.example.com/ssrblazor:latest
    docker push registry.example.com/ssrblazor:latest
```

## Testing the Docker Image

### Verify libgdiplus is installed

```bash
docker run --rm ssrblazor:latest bash -c "ls -la /usr/lib/*/libgdiplus.so*"
```

### Test report generation

After starting the container, navigate to the report feature and generate a test report to verify FastReport works correctly with libgdiplus.

## Common Commands

```bash
# Build image
docker-compose -f SSRBlazor/docker-compose.yaml build

# Start services
docker-compose -f SSRBlazor/docker-compose.yaml up -d

# View logs
docker-compose -f SSRBlazor/docker-compose.yaml logs -f

# Stop services
docker-compose -f SSRBlazor/docker-compose.yaml down

# Rebuild and restart
docker-compose -f SSRBlazor/docker-compose.yaml up --build

# Remove all containers and images
docker-compose -f SSRBlazor/docker-compose.yaml down --rmi all
```

## Image Size Optimization

The multi-stage build keeps the final image size small by:
- Using the minimal `aspnet` runtime image (not SDK)
- Cleaning apt cache after installing packages
- Not including build tools in the final image

Expected image size: ~250-350 MB

## Security Considerations

1. **Run as non-root user**: The Dockerfile uses `USER $APP_UID`
2. **Minimal attack surface**: Only runtime dependencies installed
3. **Regular updates**: Base images should be regularly updated
4. **Secrets management**: Use Docker secrets or environment variables, never hardcode

## Next Steps

1. Test the Docker build locally
2. Verify report generation works in the container
3. Set up container registry for production deployments
4. Configure orchestration (Kubernetes, Docker Swarm, etc.) if needed
5. Set up monitoring and logging for containerized deployments
