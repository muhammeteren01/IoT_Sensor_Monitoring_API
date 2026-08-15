using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Validations.Measurements;

public class CreateSensorMeasurementRequestValidator : AbstractValidator<CreateSensorMeasurementRequest>
{
    public CreateSensorMeasurementRequestValidator()
    {
        RuleFor(x => x.SensorId)
            .NotEmpty().WithMessage("SensorId is required.");

        RuleFor(x => x)
            .Must(HasAtLeastOneMetric)
            .WithMessage("At least one measurement value is required.");

        RuleFor(x => x.Temperature)
            .InclusiveBetween(-80, 200).WithMessage("Temperature must be between -80 and 200.")
            .When(x => x.Temperature.HasValue);

        RuleFor(x => x.Humidity)
            .InclusiveBetween(0, 100).WithMessage("Humidity must be between 0 and 100.")
            .When(x => x.Humidity.HasValue);

        RuleFor(x => x.Pressure)
            .GreaterThan(0).WithMessage("Pressure must be greater than 0.")
            .When(x => x.Pressure.HasValue);

        RuleFor(x => x.BatteryLevel)
            .InclusiveBetween(0, 100).WithMessage("Battery level must be between 0 and 100.")
            .When(x => x.BatteryLevel.HasValue);

        RuleFor(x => x.SignalStrength)
            .InclusiveBetween(-150, 0).WithMessage("Signal strength must be between -150 and 0 dBm.")
            .When(x => x.SignalStrength.HasValue);
    }

    private static bool HasAtLeastOneMetric(CreateSensorMeasurementRequest request) =>
        request.Temperature.HasValue
        || request.Humidity.HasValue
        || request.Pressure.HasValue
        || request.BatteryLevel.HasValue
        || request.SignalStrength.HasValue;
}
