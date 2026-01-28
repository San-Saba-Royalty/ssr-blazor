# Stage 1: Runtime Base (Ubuntu-based in .NET 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root

# Install libgdiplus and essential drawing dependencies for Ubuntu 24.04
RUN apt-get update && \
    apt-get install -y --no-install-recommends libgdiplus libc6-dev libx11-6 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Switch back to the default non-root user provided by .NET images
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files using relative paths from the solution root
COPY ["SSRBlazor/SSRBlazor.csproj", "SSRBlazor/"]
COPY ["SSRBusiness.NET10/SSRBusiness.csproj", "SSRBusiness.NET10/"]
COPY ["DocSharp/src/DocSharp.Binary/DocSharp.Binary.Common/DocSharp.Binary.Common.csproj", "DocSharp/src/DocSharp.Binary/DocSharp.Binary.Common/"]
COPY ["DocSharp/src/DocSharp.Binary/DocSharp.Binary.Doc/DocSharp.Binary.Doc.csproj", "DocSharp/src/DocSharp.Binary/DocSharp.Binary.Doc/"]

# Restore dependencies
RUN dotnet restore "SSRBlazor/SSRBlazor.csproj"

# Copy all source files
COPY . .

# Build the main project
WORKDIR "/src/SSRBlazor"
RUN dotnet build "SSRBlazor.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "SSRBlazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# IMPORTANT: Enable Unix support for System.Drawing if still using it
ENV DOTNET_System_Drawing_EnableUnixSupport=true

ENTRYPOINT ["dotnet", "SSRBlazor.dll"]
