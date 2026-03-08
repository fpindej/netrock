# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-wide config files
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["nuget.config", "."]

# Copy csproj files
COPY ["Test.Shared/Test.Shared.csproj", "Test.Shared/"]
COPY ["Test.Domain/Test.Domain.csproj", "Test.Domain/"]
COPY ["Test.Application/Test.Application.csproj", "Test.Application/"]
COPY ["Test.Infrastructure/Test.Infrastructure.csproj", "Test.Infrastructure/"]
COPY ["Test.ServiceDefaults/Test.ServiceDefaults.csproj", "Test.ServiceDefaults/"]
COPY ["Test.WebApi/Test.WebApi.csproj", "Test.WebApi/"]
COPY ["HealthProbe/HealthProbe.csproj", "HealthProbe/"]

# Restore dependencies
RUN dotnet restore "Test.WebApi/Test.WebApi.csproj" \
    && dotnet restore "HealthProbe/HealthProbe.csproj"

# Copy the entire source code
COPY . .

# Build the application
WORKDIR "/src/Test.WebApi"
RUN dotnet build "Test.WebApi.csproj" -c Release

# Publish stage
FROM build AS publish

# StripDevConfig controls whether appsettings.Development/Testing.json are excluded.
# Default: true (production). Pass STRIP_DEV_CONFIG=false as a build arg to keep them.
ARG STRIP_DEV_CONFIG=true

RUN dotnet publish "Test.WebApi.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false \
    /p:StripDevConfig=${STRIP_DEV_CONFIG} \
    && dotnet publish "/src/HealthProbe/HealthProbe.csproj" -c Release -o /app/healthprobe \
    /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false

# Defense-in-depth: strip non-production config even if .csproj exclusion is bypassed
RUN if [ "$STRIP_DEV_CONFIG" = "true" ]; then \
        rm -f /app/publish/appsettings.Development.json /app/publish/appsettings.Testing.json; \
    fi

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/healthprobe ./healthprobe/

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Test.WebApi.dll"]
