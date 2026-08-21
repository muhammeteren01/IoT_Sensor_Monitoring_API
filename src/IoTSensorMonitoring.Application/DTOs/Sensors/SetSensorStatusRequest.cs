using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Sensors;

public record SetSensorStatusRequest(SensorStatus Status);
