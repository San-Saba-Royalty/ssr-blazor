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

# Install git to clone dependencies
RUN apt-get update && \
    apt-get install -y git && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Clone external dependencies
RUN git clone https://github.com/San-Saba-Royalty/ssr-business-net10.git SSRBusiness.NET10
RUN git clone https://github.com/GQAdonis/DocSharp.git DocSharp

# Copy the SSRBlazor project file first for better layer caching
COPY ["SSRBlazor.csproj", "SSRBlazor/"]

# Restore dependencies (this will now find the cloned projects)
RUN dotnet restore "SSRBlazor/SSRBlazor.csproj"

# Copy all SSRBlazor source files (the context is the current SSRBlazor directory)
COPY . SSRBlazor/

# Build the main project
WORKDIR "/src/SSRBlazor"
RUN dotnet build "SSRBlazor.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
WORKDIR "/src/SSRBlazor"
RUN dotnet publish "SSRBlazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# IMPORTANT: Enable Unix support for System.Drawing if still using it
ENV DOTNET_System_Drawing_EnableUnixSupport=true

ENTRYPOINT ["dotnet", "SSRBlazor.dll"]
