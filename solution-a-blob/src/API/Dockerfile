FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project
COPY ["shared/models/DeviceManifest.Shared.csproj", "shared/models/"]

# Copy API project
COPY ["solution-a-blob/src/API/DeviceManifest.Api.csproj", "solution-a-blob/src/API/"]

RUN dotnet restore "solution-a-blob/src/API/DeviceManifest.Api.csproj"

# Copy all source files
COPY shared/models/ shared/models/
COPY solution-a-blob/src/API/ solution-a-blob/src/API/

WORKDIR "/src/solution-a-blob/src/API"
RUN dotnet build "DeviceManifest.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DeviceManifest.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Run as non-root user
USER $APP_UID

ENTRYPOINT ["dotnet", "DeviceManifest.Api.dll"]
