# Docker Build Configuration Notes

## Dependencies Resolution

The Dockerfile has been updated to handle external dependencies by cloning them during the build process instead of expecting them to be present in the build context.

## External Repositories

The following repositories are cloned during the Docker build:

1. **SSRBusiness.NET10**: `https://github.com/San-Saba-Royalty/ssr-business-net10.git`
   - Contains business logic layer
   - Cloned to `/src/SSRBusiness.NET10` in the build container

2. **DocSharp**: `https://github.com/GQAdonis/DocSharp.git`
   - Document processing library
   - Cloned to `/src/DocSharp` in the build container

## Build Process

1. **Git Installation**: The build stage installs git to enable repository cloning
2. **Repository Cloning**: External dependencies are cloned into the build context
3. **Project Restore**: Dependencies are restored using the cloned repositories
4. **Source Copy**: Main application source files are copied
5. **Build & Publish**: Standard .NET build and publish process

## Build Commands

### Local Development
```bash
# Build locally (from SSRBlazor directory)
docker build -t ssrblazor:local .

# Build and run
docker build -t ssrblazor:local .
docker run -p 8080:8080 ssrblazor:local
```

### Production (CI/CD)
The GitHub Actions workflow automatically:
1. Builds the Docker image with proper caching
2. Pushes to Docker Hub
3. Deploys to Kubernetes using OpenTofu

## Security Considerations

- Repositories are cloned using HTTPS (no SSH keys required)
- Public repositories only - no authentication needed
- Build-time dependencies only (not included in final image)
- Standard .dockerignore prevents sensitive files from being copied

## Troubleshooting

If build fails:
1. Check that external repositories are accessible
2. Verify git is available in the build environment
3. Ensure network connectivity for git clone operations
4. Check project references in .csproj files match cloned directory structure

## Performance Notes

- Git clone operations are cached in Docker layers
- Dependency restoration benefits from Docker layer caching
- Build artifacts use multi-stage builds for smaller final image
- External dependencies add ~30-60 seconds to initial build time