FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY IoTSensorMonitoring.sln ./
COPY src/IoTSensorMonitoring.Domain/IoTSensorMonitoring.Domain.csproj src/IoTSensorMonitoring.Domain/
COPY src/IoTSensorMonitoring.Application/IoTSensorMonitoring.Application.csproj src/IoTSensorMonitoring.Application/
COPY src/IoTSensorMonitoring.Infrastructure/IoTSensorMonitoring.Infrastructure.csproj src/IoTSensorMonitoring.Infrastructure/
COPY src/IoTSensorMonitoring.Worker/IoTSensorMonitoring.Worker.csproj src/IoTSensorMonitoring.Worker/

RUN dotnet restore src/IoTSensorMonitoring.Worker/IoTSensorMonitoring.Worker.csproj

COPY src/IoTSensorMonitoring.Domain/ src/IoTSensorMonitoring.Domain/
COPY src/IoTSensorMonitoring.Application/ src/IoTSensorMonitoring.Application/
COPY src/IoTSensorMonitoring.Infrastructure/ src/IoTSensorMonitoring.Infrastructure/
COPY src/IoTSensorMonitoring.Worker/ src/IoTSensorMonitoring.Worker/

RUN dotnet publish src/IoTSensorMonitoring.Worker/IoTSensorMonitoring.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IoTSensorMonitoring.Worker.dll"]
