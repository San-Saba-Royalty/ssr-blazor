# Base runtime image with libgdiplus installed
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# Install libgdiplus for FastReport/System.Drawing support
RUN apt-get update && \
    apt-get install -y libgdiplus && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for both SSRBlazor and SSRBusiness
COPY ["SSRBlazor/SSRBlazor.csproj", "SSRBlazor/"]
COPY ["SSRBusiness.NET10/SSRBusiness.csproj", "SSRBusiness.NET10/"]

# Restore dependencies
RUN dotnet restore "SSRBlazor/SSRBlazor.csproj"

# Copy all source files
COPY ["SSRBlazor/", "SSRBlazor/"]
COPY ["SSRBusiness.NET10/", "SSRBusiness.NET10/"]

# Build the project
WORKDIR "/src/SSRBlazor"
RUN dotnet build "./SSRBlazor.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SSRBlazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SSRBlazor.dll"]
