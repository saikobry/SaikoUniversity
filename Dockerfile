# Build stage
# Restores first against project files only, so a source-only change reuses the cached NuGet layer.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/SaikoUniversity.Domain/SaikoUniversity.Domain.csproj          src/SaikoUniversity.Domain/
COPY src/SaikoUniversity.Application/SaikoUniversity.Application.csproj src/SaikoUniversity.Application/
COPY src/SaikoUniversity.Infrastructure/SaikoUniversity.Infrastructure.csproj src/SaikoUniversity.Infrastructure/
COPY src/SaikoUniversity.Api/SaikoUniversity.Api.csproj                src/SaikoUniversity.Api/
RUN dotnet restore src/SaikoUniversity.Api/SaikoUniversity.Api.csproj

COPY src/ src/
RUN dotnet publish src/SaikoUniversity.Api/SaikoUniversity.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
# aspnet image already carries the shared framework, so the publish output stays small.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1

EXPOSE 8080

COPY --from=build /app/publish .

# Runs unprivileged. The base image ships an `app` user for exactly this.
USER app

ENTRYPOINT ["dotnet", "SaikoUniversity.Api.dll"]
