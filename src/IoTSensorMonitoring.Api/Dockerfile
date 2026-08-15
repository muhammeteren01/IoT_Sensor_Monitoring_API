FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY IoTSensorMonitoring.sln ./
COPY src/IoTSensorMonitoring.Domain/IoTSensorMonitoring.Domain.csproj src/IoTSensorMonitoring.Domain/
COPY src/IoTSensorMonitoring.Application/IoTSensorMonitoring.Application.csproj src/IoTSensorMonitoring.Application/
COPY src/IoTSensorMonitoring.Infrastructure/IoTSensorMonitoring.Infrastructure.csproj src/IoTSensorMonitoring.Infrastructure/
COPY src/IoTSensorMonitoring.Api/IoTSensorMonitoring.Api.csproj src/IoTSensorMonitoring.Api/

RUN dotnet restore src/IoTSensorMonitoring.Api/IoTSensorMonitoring.Api.csproj

COPY src/IoTSensorMonitoring.Domain/ src/IoTSensorMonitoring.Domain/
COPY src/IoTSensorMonitoring.Application/ src/IoTSensorMonitoring.Application/
COPY src/IoTSensorMonitoring.Infrastructure/ src/IoTSensorMonitoring.Infrastructure/
COPY src/IoTSensorMonitoring.Api/ src/IoTSensorMonitoring.Api/

RUN dotnet publish src/IoTSensorMonitoring.Api/IoTSensorMonitoring.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IoTSensorMonitoring.Api.dll"]
