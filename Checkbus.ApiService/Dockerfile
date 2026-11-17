# Build context must be the repository root (Railway: set Root Directory to "/" and
# Dockerfile Path to "Checkbus.ApiService/Dockerfile"), since this project references
# sibling projects outside its own folder.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# Pre-create the upload directory and hand it to the non-root app user before any volume gets
# mounted on it - a named volume mounted on a path that doesn't exist in the image yet is created
# root-owned, and this container never runs as root.
RUN mkdir -p /app/App_Data/storage && chown -R $APP_UID:$APP_UID /app/App_Data
USER $APP_UID
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
ENTRYPOINT ["dotnet", "Checkbus.ApiService.dll"]
