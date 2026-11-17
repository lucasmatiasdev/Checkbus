# Build context must be the repository root (Railway: set Root Directory to "/" and
# Dockerfile Path to "Checkbus.Web/Dockerfile"), since this project references a
# sibling project outside its own folder.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Checkbus.Web/Checkbus.Web.csproj", "Checkbus.Web/"]
COPY ["Checkbus.ServiceDefaults/Checkbus.ServiceDefaults.csproj", "Checkbus.ServiceDefaults/"]
RUN dotnet restore "Checkbus.Web/Checkbus.Web.csproj"

COPY Checkbus.Web/ Checkbus.Web/
COPY Checkbus.ServiceDefaults/ Checkbus.ServiceDefaults/

WORKDIR /src/Checkbus.Web
RUN dotnet build "Checkbus.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Checkbus.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Checkbus.Web.dll"]
