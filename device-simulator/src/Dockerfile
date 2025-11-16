FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project
COPY ["shared/models/DeviceManifest.Shared.csproj", "shared/models/"]

# Copy simulator project
COPY ["device-simulator/src/DeviceSimulator.csproj", "device-simulator/src/"]

RUN dotnet restore "device-simulator/src/DeviceSimulator.csproj"

# Copy all source files
COPY shared/models/ shared/models/
COPY device-simulator/src/ device-simulator/src/

WORKDIR "/src/device-simulator/src"
RUN dotnet build "DeviceSimulator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DeviceSimulator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DeviceSimulator.dll"]
