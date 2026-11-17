# Build context must be the repository root (Railway: set Root Directory to "/" and
# Dockerfile Path to "Checkbus.ApiService/Dockerfile"), since this project references
# sibling projects outside its own folder.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# Pre-create the upload directory for the no-volume case. When a Railway Volume IS mounted on
# /app/App_Data/storage, Railway creates its backing storage root-owned regardless of this
# chown, so the container must still start as root (no USER here) and let
# docker-entrypoint.sh re-chown it on every startup before dropping privileges to the
# non-root "app" user that ships in the .NET 8+ base images.
RUN mkdir -p /app/App_Data/storage && chown -R $APP_UID:$APP_UID /app/App_Data
COPY Checkbus.ApiService/docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Checkbus.ApiService/Checkbus.ApiService.csproj", "Checkbus.ApiService/"]
COPY ["Checkbus.ApiService.Application/Checkbus.ApiService.Application.csproj", "Checkbus.ApiService.Application/"]
COPY ["Checkbus.ApiService.Domain/Checkbus.ApiService.Domain.csproj", "Checkbus.ApiService.Domain/"]
COPY ["Checkbus.ApiService.Infrastructure/Checkbus.ApiService.Infrastructure.csproj", "Checkbus.ApiService.Infrastructure/"]
COPY ["Checkbus.ServiceDefaults/Checkbus.ServiceDefaults.csproj", "Checkbus.ServiceDefaults/"]
RUN dotnet restore "Checkbus.ApiService/Checkbus.ApiService.csproj"

COPY Checkbus.ApiService/ Checkbus.ApiService/
COPY Checkbus.ApiService.Application/ Checkbus.ApiService.Application/
COPY Checkbus.ApiService.Domain/ Checkbus.ApiService.Domain/
COPY Checkbus.ApiService.Infrastructure/ Checkbus.ApiService.Infrastructure/
COPY Checkbus.ServiceDefaults/ Checkbus.ServiceDefaults/

WORKDIR /src/Checkbus.ApiService
RUN dotnet build "Checkbus.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Checkbus.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["docker-entrypoint.sh"]
